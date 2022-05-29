using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Amazon.DynamoDBv2.DataModel;
using Amazon.Runtime.Internal.Util;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace Notes.WebAPI.Controllers
{
    [Route("api/[controller]")]
    public class NotesController : ControllerBase
    {
        private readonly CryptoContext _context;

        [FromHeader]
        public string DbKey { get; set; }

        public NotesController(CryptoContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<List<Note>> Get()
        {
            return await _context.QueryAsync(DbKey);
        }

        [HttpGet("{id}")]
        [ProducesResponseType(200, Type = typeof(Note))]
        public async Task<IActionResult> Get(string id)
        {
            var note = await _context.LoadAsync(DbKey, id);
            if (note == null) return NotFound();

            return Ok(note);
        }

        public class StringData
        {
            public string Data { get; set; }
        }

        [HttpPost]
        public async Task<string> Post([FromBody]StringData value)
        {
            var note = new Note();
            note.Body = value.Data;

            await _context.SaveAsync(note, DbKey);

            return note.Id;
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Put(string id, [FromBody]StringData value)
        {
            var note = await _context.LoadAsync(DbKey, id);
            if (note == null) return NotFound();

            note.Body = value.Data;

            await _context.SaveAsync(note, DbKey);

            return Ok();
        }

        [HttpDelete("{id}")]
        public async Task Delete(string id)
        {
            await _context.DeleteAsync(DbKey, id);
        }
    }

    public class CryptoContext
    {
        private readonly Crypto _crypto;
        private readonly IDynamoDBContext _context;

        public CryptoContext(Crypto crypto, IDynamoDBContext context)
        {
            _crypto = crypto;
            _context = context;
        }

        public async Task SaveAsync(Note note, string password)
        {
            var json = JsonConvert.SerializeObject(note);

            var dbRec = new DBRecord();
            dbRec.HashKey = _crypto.Hash(password);
            dbRec.RangeKey = note.Id;

            var key = _crypto.Hash(password, dbRec.RangeKey);
            dbRec.Data = _crypto.Encrypt(key, out var iv, json);
            dbRec.IV = iv;

            await _context.SaveAsync(dbRec);
        }

        public async Task<Note> LoadAsync(string password, string rangeKey)
        {
            var hashKey = _crypto.Hash(password);
            var dbRec = await _context.LoadAsync<DBRecord>(hashKey, rangeKey);
            if (dbRec == null) return null;

            return Decrypt(password, dbRec);
        }

        private Note Decrypt(string password, DBRecord dbRec)
        {
            var key = _crypto.Hash(password, dbRec.RangeKey);
            var json = _crypto.Decrypt(key, dbRec.IV, dbRec.Data);

            return JsonConvert.DeserializeObject<Note>(json);
        }

        public async Task DeleteAsync(string password, string rangeKey)
        {
            var hashKey = _crypto.Hash(password);
            await _context.DeleteAsync<DBRecord>(hashKey, rangeKey);
        }

        public async Task<List<Note>> QueryAsync(string password)
        {
            var hashKey = _crypto.Hash(password);
            var scan = _context.QueryAsync<DBRecord>(hashKey);
            var recs = await scan.GetRemainingAsync();

            var list = new List<Note>();
            foreach (var rec in recs)
            {
                list.Add(Decrypt(password, rec));
            }

            return list;
        }
    }

    public class Crypto
    {
        private readonly IDynamoDBContext _context;

        public Crypto(IDynamoDBContext context)
        {
            _context = context;
        }

        public string Encode(byte[] bytes) => Convert.ToBase64String(bytes);
        public byte[] Decode(string data) => Convert.FromBase64String(data);

        public string Hash(params string[] data)
        {
            var hash = SHA256.Create();

            using var ms = new MemoryStream();
            using (var sw = new StreamWriter(ms))
            {
                foreach (var datum in data)
                {
                    sw.Write(datum);
                }
            }

            var bytes = ms.ToArray();

            var hashed = hash.ComputeHash(bytes);

            return Encode(hashed);
        }

        public string Encrypt(string key, out string iv, string data)
        {
            using var aes = Aes.Create();
            var aesKey = Decode(key);
            aes.Key = aesKey;
            iv = Encode(aes.IV);

            using var ms = new MemoryStream();
            using (var cs = new CryptoStream(ms, aes.CreateEncryptor(), CryptoStreamMode.Write))
            {
                using var sw = new StreamWriter(cs);
                sw.Write(data);
            }

            return Encode(ms.ToArray());
        }

        public string Decrypt(string key, string iv, string data)
        {
            using var aes = Aes.Create();
            aes.Key = Decode(key);
            aes.IV = Decode(iv);

            using var ms = new MemoryStream(Decode(data));
            using var cs = new CryptoStream(ms, aes.CreateDecryptor(), CryptoStreamMode.Read);
            using var sr = new StreamReader(cs);
            
            return sr.ReadToEnd();
        }
    }

    public class Note
    {
        public string Id { get; set; } = Guid.NewGuid().ToString("N");
        public string Body { get; set; }
        public DateTime Created { get; set; } = DateTime.Now;
    }

    [DynamoDBTable("notes-2022-05-29")]
    public class DBRecord
    {
        [DynamoDBHashKey]
        public string HashKey { get; set; }

        [DynamoDBRangeKey]
        public string RangeKey { get; set; }
        
        public string Data { get; set; }
        
        public string IV { get; set; }
    }
}

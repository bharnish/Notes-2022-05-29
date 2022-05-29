using System.Collections.Generic;
using System.Threading.Tasks;
using Amazon.DynamoDBv2.DataModel;
using Newtonsoft.Json;
using Notes.WebAPI.Domain;

namespace Notes.WebAPI.Services
{
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
}
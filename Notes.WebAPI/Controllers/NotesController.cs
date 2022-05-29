using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Amazon.Runtime.Internal.Util;
using Microsoft.AspNetCore.Mvc;
using Notes.WebAPI.Domain;
using Notes.WebAPI.Services;

namespace Notes.WebAPI.Controllers
{
    [Route("api/[controller]")]
    public class NotesController : ControllerBase
    {
        public class StringData
        {
            public string Data { get; set; }
        }

        private readonly CryptoContext _context;

        [FromHeader]
        public string DbKey { get; set; }

        public NotesController(CryptoContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IEnumerable<Note>> Get()
        {
            return (await _context.QueryAsync(DbKey)).OrderByDescending(x => x.Created);
        }

        [HttpGet("{id}")]
        [ProducesResponseType(200, Type = typeof(Note))]
        public async Task<IActionResult> Get(string id)
        {
            var note = await _context.LoadAsync(DbKey, id);
            if (note == null) return NotFound();

            return Ok(note);
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
}

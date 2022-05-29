using Amazon.DynamoDBv2.DataModel;

namespace Notes.WebAPI.Domain
{
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
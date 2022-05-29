# Notes-2022-05-29

Securely store notes.

Choose a unique dbkey to store your notes under.

Each note is stored in AWS DynamoDB with a hashkey and rangekey.

The hashkey is a SHA256 hash of the dbkey you chose. The rangekey is a hash of the dbkey and the record's unqique Id. Each record is encrypted using a password which is a 
hash of the dbkey + rangekey, and a random IV using AES.

namespace rickhelper
{
    public class HashedFile
    {
        public string File { get; set; }
        public string Hash { get; set; }
        public long Length { get; set; }

        public HashedFile() { }
        public HashedFile(string csvLine)
        {
            var parts = csvLine.Split(";");
            File = parts[0];
            Hash = parts[1];
            Length = long.Parse(parts[2]);
        }
    }
}

namespace ethernetip
{
    public class Tag
    {
        public string Name { get; }
        public object? Value { get; set; }

        public Tag(string name)
        {
            Name = name;
        }
    }
}
namespace XTC.FMP.APP.Blazor
{
    public class RepoAgent
    {
        public string Org { get; set; }
        public string Name { get; set; }
        public string Version { get; set; }
        public int Port { get; set; }
        public string[] Pages { get; set; } = new string[0];
    }

}

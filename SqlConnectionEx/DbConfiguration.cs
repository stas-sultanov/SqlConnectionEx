namespace SqlConnectionEx
{
	public class DbConfiguration
	{
		public int ConnectionTimeout { get; set; } = 30;

		public string MainConnectionString { get; set; }

		public string ReadConnectionString { get; set; }

		public string DependencyTypeName { get; } = "MySQL";
	}
}

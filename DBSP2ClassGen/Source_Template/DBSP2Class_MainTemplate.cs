namespace SyncnetPlatform
{
	namespace database
	{
		namespace db_%%DATABASE_NAME%% 
		{			
			public class dbclass_%%DATABASE_NAME%%
			{
				protected SqlConnection conn;
				protected SqlCommand cmd;
		
				protected bool IsCmdSuccess;
				protected string ErrorMessage; 
		
				public dbclass_%%DATABASE_NAME%%()
				{
					string connString = "server=%%SERVER_IP%%,%%SERVER_PORT%%;uid=%%USERNAME%%;pwd=%%PASSWORD%%;database=%%DATABASE_NAME%%";
					conn = new SqlConnection(connString);
					cmd = new SqlCommand();            
					cmd.Connection = conn;
					IsCmdSuccess = true; 
				}
				public bool IsOK()
				{
					return IsCmdSuccess;
				}
				public string GetLastError()
				{
					return ErrorMessage; 
				}	
			}
			
			%%DBSP2CLASSES%% 
		}
	}
}
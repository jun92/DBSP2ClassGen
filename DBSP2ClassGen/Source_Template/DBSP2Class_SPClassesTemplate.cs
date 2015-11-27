public class %%SP_NAME%% : dbclass_%%DATABASE_NAME%%
{
	public List<Dictionary<string, dynamic>> r;	
	public int _ReturnValue;
	
%%DECLARE_OUTPUT_PARAM%%
	
	public %%SP_NAME%%( %%DECLARE_METHOD_PARAMS%% )
	{
		this.cmd.CommandType = System.Data.CommandType.StoredProcedure;
		this.cmd.CommandText = "%%SP_NAME%%";
		
		this.r = new List<Dictionary<string, dynamic>>();
		
%%SQLPARAM_INIT%% 		
		
		SqlParameter sp_return_val = new SqlParameter("@ReturnValue", 0);
        sp_return_val.Direction = ParameterDirection.ReturnValue;
		
%%SQLPARAM_ADD%% 
		this.cmd.Parameters.Add(sp_return_val);
		
		try
        {
			this.conn.Open();
			SqlDataReader sdr = this.cmd.ExecuteReader();
			if (sdr.HasRows)
			{
				DataTable s = sdr.GetSchemaTable();
				List<string> p = new List<string>();
				foreach (DataRow row in s.Rows)
				{
					foreach (DataColumn column in s.Columns) if (0 == column.ColumnName.CompareTo("ColumnName")) p.Add((string)row[column]);
				}
				while (sdr.Read())
				{
					Dictionary<string, dynamic> row1 = new Dictionary<string, dynamic>();
					for (int i = 0; i < p.Count(); i++) { row1[p[i]] = sdr[p[i]]; }
					r.Add(row1);
					row1 = null;
				}
			}
			sdr.Close();
			conn.Close();
			
%%SAVE_OUTPUT_PARAM%% 
			this._ReturnValue = (int)sp_return_val.Value;
			return;
		}
		catch (Exception ex)
		{
			IsCmdSuccess = false;
			ErrorMessage = ex.Message; 
		}
	}
}
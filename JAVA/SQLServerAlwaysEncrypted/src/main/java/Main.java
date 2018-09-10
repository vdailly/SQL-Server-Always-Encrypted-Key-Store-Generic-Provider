import java.sql.Connection;
import java.sql.DriverManager;
import java.sql.PreparedStatement;
import java.sql.ResultSet;
import java.sql.SQLException;
import java.util.HashMap;
import java.util.Map;

import com.microsoft.sqlserver.jdbc.SQLServerColumnEncryptionJavaKeyStoreProvider;
import com.microsoft.sqlserver.jdbc.SQLServerColumnEncryptionKeyStoreProvider;
import com.microsoft.sqlserver.jdbc.SQLServerConnection;
import com.microsoft.sqlserver.jdbc.SQLServerException;

public class Main {
	
	//connectionString example: "jdbc:sqlserver://192.168.0.1:1433;databaseName=CLINIC;user=admin;password=P@ssw0rd;columnEncryptionSetting=Enabled");
	public static String connectionString = "";
	public static String keyStoreLocation = "C:\\Temp\\CLINIC_CMK_GENERIC.pfx";
	public static String keyStoreSecret = "P@ssw0rd";
	
	//the keyPath as referred in https://docs.microsoft.com/fr-fr/sql/connect/jdbc/using-always-encrypted-with-the-jdbc-driver?view=sql-server-2017#creating-a-column-master-key-for-the-java-key-store
	//should be the "alias = Friendly Name" of the certificate
	public static String keyPath = "CLINIC_CMK_GENERIC";
	
	public static void main(String[] args) {
		
		GetConnectionString(args);
		Connection connection = CreateConnection(connectionString);
		RegisterGenericProvider(connection);
		ReadDataEncrypted(connection);
	}
	
	public static void GetConnectionString(String[] args) {
		if (null == args)
			System.out.println("Provide a valid JDBC SQL Server connection string");
		if (args.length > 1)
			System.out.println("Provide only one valid JDBC SQL Server connection string");
		connectionString = args[0];
	}
	
	
	public static Connection CreateConnection(String connectionUrl) {
		try {
		    System.out.print("Connecting to SQL Server ... ");
		    Connection connection = DriverManager.getConnection(connectionUrl);
		        System.out.println("Done.");
		        return connection;
		} catch (Exception e) {
		    e.printStackTrace();
		}
		return null;
	}

	public static void ReadDataEncrypted(Connection connection) {
		try {		
			PreparedStatement selectStatement = connection
					.prepareStatement("SELECT [SSN], [FirstName], [LastName], [BirthDate] FROM [dbo].[Patients] WHERE SSN = ?;");
		    selectStatement.setString(1, "795-73-9838");
		    
		    System.out.println("Executing SQL Query ...");
		    ResultSet rs = selectStatement.executeQuery();
		    while (rs.next()) {
		        System.out.println("SSN: " + rs.getString("SSN") + ", FirstName: " + rs.getString("FirstName") + ", LastName:"
		                + rs.getString("LastName") + ", Date of Birth: " + rs.getString("BirthDate"));
		    }
		    System.out.println("Done.");
		}

		catch (SQLException e) {
		    e.printStackTrace();
		}
	}
	
	public static void RegisterGenericProvider(Connection connection) {
		
		Map<String, SQLServerColumnEncryptionKeyStoreProvider> map = new HashMap<String,SQLServerColumnEncryptionKeyStoreProvider>();
		
		
		//and the path to our certificate in the file system and the password to get the private key.
		SQLServerColumnEncryptionJavaKeyStoreProvider provider;
		try {
			provider = new SQLServerColumnEncryptionJavaKeyStoreProvider(keyStoreLocation, keyStoreSecret.toCharArray());
		
			//create our GenericKeyStoreProvider with the alias (Friendly Name of the certificate) CLINIC_CMK_GENERIC as keyPath.
			//because the wrapped JavaKeyStoreProvider normally use the this value to get the certificate from store.
			SQLServerColumnEncryptionGenericKeyStoreProvider genericprovider = new SQLServerColumnEncryptionGenericKeyStoreProvider(provider, keyPath);
			
			map.put(genericprovider.getName(), genericprovider);
			SQLServerConnection.registerColumnEncryptionKeyStoreProviders(map);
			
		} catch (SQLServerException e) {
			e.printStackTrace();
		}	
	}
}
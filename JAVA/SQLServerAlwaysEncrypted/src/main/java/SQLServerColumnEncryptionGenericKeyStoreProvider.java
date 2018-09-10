import com.microsoft.sqlserver.jdbc.SQLServerColumnEncryptionKeyStoreProvider;
import com.microsoft.sqlserver.jdbc.SQLServerException;

public class SQLServerColumnEncryptionGenericKeyStoreProvider extends SQLServerColumnEncryptionKeyStoreProvider {
	
	String name = "GENERIC";
	String keypath = "";
	SQLServerColumnEncryptionKeyStoreProvider provider;
	
	///The KeyPath should match the underlying provider wrapped by this custom provider.
	///In case this an underlying SQLServerColumnEncryptionJavaKeyStoreProvider, the KeyPath is the alias of the certificate (alias = Friendly Name of the certificate)
	public SQLServerColumnEncryptionGenericKeyStoreProvider(SQLServerColumnEncryptionKeyStoreProvider provider, String KeyPath) {
		this.provider = provider;
		this.keypath = KeyPath;
	}
	
	@Override
	public void setName(String name) {
		this.name = name;
	}

	@Override
	public String getName() {
		return this.name;
	}
	
	public void setPath(String KeyPath) {
		this.keypath = KeyPath;
	}
	
	public String getPath() {
		return this.keypath;
	}

	@Override
	public byte[] decryptColumnEncryptionKey(String masterKeyPath, String encryptionAlgorithm,
			byte[] encryptedColumnEncryptionKey) throws SQLServerException {
		return provider.decryptColumnEncryptionKey(keypath, encryptionAlgorithm, encryptedColumnEncryptionKey);
	}

	@Override
	public byte[] encryptColumnEncryptionKey(String masterKeyPath, String encryptionAlgorithm,
			byte[] columnEncryptionKey) throws SQLServerException {
		return provider.encryptColumnEncryptionKey(keypath, encryptionAlgorithm, columnEncryptionKey);
	}

}

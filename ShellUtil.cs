using Microsoft.Data.Sqlite;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Text;

namespace GOG_Intergalactic;

public class ShellUtil
{
	private readonly string _connectionString = "Data Source=C:\\ProgramData\\GOG.com\\Galaxy\\storage\\galaxy-2.0.db";

	public async Task CreateShortcut(string productId)
	{
		await using var connection = new SqliteConnection(_connectionString);
		await connection.OpenAsync();

		await using var command = connection.CreateCommand();
		command.CommandText =
			"SELECT installationPath, label FROM InstalledBaseProducts " +
			"JOIN ProductsToReleaseKeys ON InstalledBaseProducts.productId = ProductsToReleaseKeys.gogId " +
			"JOIN PlayTasks ON ProductsToReleaseKeys.releaseKey = PlayTasks.gameReleaseKey " +
			"JOIN PlayTaskLaunchParameters ON PlayTasks.id = PlayTaskLaunchParameters.playTaskId " +
			"WHERE productId = $productId AND isPrimary = 1";
		command.Parameters.AddWithValue("productId", productId);

		await using var reader = await command.ExecuteReaderAsync();
		if (await reader.ReadAsync())
		{
			// TODO: Find GalaxyClient installation path (https://stackoverflow.com/questions/909910/how-to-find-the-execution-path-of-a-installed-software)
			var galaxyDirectory = "C:\\Program Files (x86)\\GOG Galaxy";
			var galaxyClient = Path.Combine(galaxyDirectory, "GalaxyClient.exe");
			string gameName = (string)reader["label"];
			gameName = gameName.Replace(":", "");

			CreateShortcut(new Shortcut
			{
				Name = gameName,
				TargetPath = galaxyClient,
				Arguments = $"/command=runGame /gameId={productId} /path=\"{(string)reader["installationPath"]}",
				WorkingDirectory = galaxyDirectory,
				IconPath = $"{(string)reader["installationPath"]}\\goggame-{productId}.ico",
			});
		}
	}

	private void CreateShortcut(Shortcut shortcut)
	{
		IShellLink link = (IShellLink)new ShellLink();

		// setup shortcut information
		link.SetPath(shortcut.TargetPath);

		if (shortcut.Arguments != null) link.SetArguments(shortcut.Arguments);
		if (shortcut.WorkingDirectory != null) link.SetWorkingDirectory(shortcut.WorkingDirectory);
		if (shortcut.IconPath != null) link.SetIconLocation(shortcut.IconPath, 0);
		if (shortcut.Description != null) link.SetDescription(shortcut.Description);

		// save it
		IPersistFile file = (IPersistFile)link;

		// TODO: Try-catch, put on User desktop instead
		string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.CommonDesktopDirectory);
		string linkPath = Path.Combine(desktopPath, $"{shortcut.Name}.lnk");
		if (!File.Exists(linkPath))
		{
			file.Save(linkPath, false);
		}
	}
}

public class Shortcut
{
	public required string Name { get; set; }
	public required string TargetPath { get; set; }
	public string? Description { get; set; }
	public string? WorkingDirectory { get; set; }
	public string? Arguments { get; set; }
	public string? IconPath { get; set; } //D:\GOG\Alan Wake\goggame-1207659037.ico
}

[ComImport]
[Guid("00021401-0000-0000-C000-000000000046")]
internal class ShellLink
{
}

[ComImport]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
[Guid("000214F9-0000-0000-C000-000000000046")]
internal interface IShellLink
{
	void GetPath([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszFile, int cchMaxPath, out IntPtr pfd, int fFlags);
	void GetIDList(out IntPtr ppidl);
	void SetIDList(IntPtr pidl);
	void GetDescription([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszName, int cchMaxName);
	void SetDescription([MarshalAs(UnmanagedType.LPWStr)] string pszName);
	void GetWorkingDirectory([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszDir, int cchMaxPath);
	void SetWorkingDirectory([MarshalAs(UnmanagedType.LPWStr)] string pszDir);
	void GetArguments([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszArgs, int cchMaxPath);
	void SetArguments([MarshalAs(UnmanagedType.LPWStr)] string pszArgs);
	void GetHotkey(out short pwHotkey);
	void SetHotkey(short wHotkey);
	void GetShowCmd(out int piShowCmd);
	void SetShowCmd(int iShowCmd);
	void GetIconLocation([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszIconPath, int cchIconPath, out int piIcon);
	void SetIconLocation([MarshalAs(UnmanagedType.LPWStr)] string pszIconPath, int iIcon);
	void SetRelativePath([MarshalAs(UnmanagedType.LPWStr)] string pszPathRel, int dwReserved);
	void Resolve(IntPtr hwnd, int fFlags);
	void SetPath([MarshalAs(UnmanagedType.LPWStr)] string pszFile);
}

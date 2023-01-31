using EncryptFile.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Threading;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace EncryptFile
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        bool _gLanguageIsEnglish = true;
        public MainWindow()
        {
            InitializeComponent();

            en_waitingGif.Visibility = Visibility.Hidden;
            de_waitingGif.Visibility = Visibility.Hidden;

            if (File.Exists(Path.Combine(Directory.GetCurrentDirectory(), "personal_rsa2048_public.key")))
                comboxPersonal.IsSelected = true;
            else
                comboxTemporary.IsSelected = true;

            fileToDecrypt.Clear();
            fileToDecrypt.Add("");
            fileToDecrypt.Add("");

            textVersion1.Text = "v" + Config.appVersion;
            textVersion2.Text = "v" + Config.appVersion;

            //get system language
            int languageId = System.Globalization.CultureInfo.CurrentCulture.LCID;
            if (languageId == 2052) //it's chinese
            {
                SelectLanguage.IsChecked = true;
                _gLanguageIsEnglish = false;
            }
            else
            {
                SelectLanguage.IsChecked = false;
                _gLanguageIsEnglish = true;
            }
        }

        static string fileToEncrypt = "";
        static List<string> fileToDecrypt = new List<string>();
        private string encryptedFileExt = ".data";
        private string encryptedkeyExt = ".key";
        private static string currentPath = "";
        private void rectEncryption_Drop(object sender, DragEventArgs e)
        {
            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
            if (files == null)
            {
                if (_gLanguageIsEnglish)
                    MessageBox.Show("File is unavailble.", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                else
                    MessageBox.Show("文件不可用。", "警告", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            foreach (string file in files)
            {
                FileAttributes attr = File.GetAttributes(file);
                if ((attr & FileAttributes.Directory) == FileAttributes.Directory)
                {
                    if (_gLanguageIsEnglish)
                        MessageBox.Show("Directory not support.", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                    else
                        MessageBox.Show("不支持对目录加密。", "警告", MessageBoxButton.OK, MessageBoxImage.Warning);
                    continue;
                }

                currentPath = Path.GetDirectoryName(file);

                en_dropTips.Visibility = Visibility.Hidden;
                en_imageIcon.Source = Models.IconHelper.icon_of_extralarge_jumbo(file);
                en_imageIcon.Visibility = Visibility.Visible;
                en_lab.Content = Path.GetFileName(file);

                //accept the first regular file.
                fileToEncrypt = file;
                break;
            }
        }
        private void rectDecryption_Drop(object sender, DragEventArgs e)
        {
            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
            if (files == null)
            {
                if (_gLanguageIsEnglish)
                    MessageBox.Show("File is unavailble.", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                else
                    MessageBox.Show("文件不可用。", "警告", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            bool hasFile = false; //only add the first ".data" file when drop multi files at one time.
            bool hasKey = false; //only add the first ".key" file when drop multi files at one time.
            foreach (string file in files)
            {
                FileAttributes attr = File.GetAttributes(file);
                if ((attr & FileAttributes.Directory) == FileAttributes.Directory)
                {
                    if (_gLanguageIsEnglish)
                        MessageBox.Show("Directory not support.", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                    else
                        MessageBox.Show("不支持对目录加密。", "警告", MessageBoxButton.OK, MessageBoxImage.Warning);
                    continue;
                }

                if (file.Substring(file.LastIndexOf('.')).ToLower() != encryptedFileExt && file.Substring(file.LastIndexOf('.')).ToLower() != encryptedkeyExt)
                {
                    if (_gLanguageIsEnglish)
                        MessageBox.Show("Only encrypted(.data) and key(.key) file support for decryption.", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                    else
                        MessageBox.Show("解密时，仅支持加密文件(.data)和密钥文件(.key)", "警告", MessageBoxButton.OK, MessageBoxImage.Warning);
                    continue;
                }

                if (!hasFile && file.Substring(file.LastIndexOf('.')).ToLower() == encryptedFileExt)
                {
                    hasFile = true;
                    currentPath = Path.GetDirectoryName(file);

                    de_dropTips.Visibility = Visibility.Hidden;
                    de_imageIcon1.Source = Models.IconHelper.icon_of_extralarge_jumbo(file);
                    de_imageIcon1.Visibility = Visibility.Visible;
                    de_lab1.Content = Path.GetFileName(file);

                    fileToDecrypt[0] = file;
                }
                else if (!hasKey && file.Substring(file.LastIndexOf('.')).ToLower() == encryptedkeyExt)
                {
                    hasKey = true;

                    de_dropTips.Visibility = Visibility.Hidden;
                    de_imageIcon2.Source = Models.IconHelper.icon_of_extralarge_jumbo(file);
                    de_imageIcon2.Visibility = Visibility.Visible;
                    de_lab2.Content = Path.GetFileName(file);

                    fileToDecrypt[1] = file;
                }
            }
        }

        private static string getRealSaveFileName(string fileName)
        {
            string dir = Path.GetDirectoryName(fileName);
            string saveFileName = Path.GetFileName(fileName);

        label_fileCheck:
            if (File.Exists(Path.Combine(dir, saveFileName)))
            {
                saveFileName = saveFileName.Substring(0, saveFileName.LastIndexOf('.')) + "(1)" + saveFileName.Substring(saveFileName.LastIndexOf('.'));
                goto label_fileCheck;
            }

            return Path.Combine(dir, saveFileName);
        }

        static int isCompleted = 0;
        static string saveFileName = "";
        private static string getShortFileName(string filePath, int reqLength)
        {
            string fileName = Path.GetFileName(filePath);

            int fullLen = System.Text.Encoding.Default.GetByteCount(fileName);

            if (fullLen <= reqLength)
                return fileName;

            string fileNameWithoutExt = fileName.Substring(0, fileName.LastIndexOf('.'));
            reqLength = reqLength - Path.GetFileName(fileName).Length;
            do
            {
                fileNameWithoutExt = fileNameWithoutExt.Substring(0, fileNameWithoutExt.Length - 1);
            } while (System.Text.Encoding.Default.GetByteCount(fileNameWithoutExt) > reqLength);

            return fileNameWithoutExt + Path.GetExtension(fileName);
        }

        static int ISEncrypt = 1;
        static int ISDecrypt = 2;
        private void doProcess(int iAction)
        {
            isCompleted = 0;

            int len_AesKey = 32;
            int len_AesIV = 16;
            int len_DecryptedString = 256;
            int len_MaxFileName = 8 * 24;

            string str_HeaderFlag = encryptedFileExt; //KEEP 5 chars
            string str_HeaderVersion = "1.0"; //KEEP 3 chars.
            int len_Header = 5 + 3; // KEEP 8 chars

            //encrypt and decrypt
            if (iAction == ISEncrypt)   //encrypt
            {
                //RSA: To encrypt AES256 KEY and IV;
                //AES: To Encrypt file data. 
                //.1 --------------- Generate AES256 key. 
                var encAlg = Models.Encrypt.GenAes();

                // .2 --------------- Combine AES KEY, AES IV, and fileName.
                byte[] buffer = new byte[len_AesKey + len_AesIV + len_MaxFileName]; //(32+16) + (8*24) = 240
                Buffer.BlockCopy(encAlg.Key, 0, buffer, 0, encAlg.Key.Length); // 32
                Buffer.BlockCopy(encAlg.IV, 0, buffer, encAlg.Key.Length, encAlg.IV.Length); // 16
                string shortFileName = getShortFileName(fileToEncrypt, len_MaxFileName);
                Buffer.BlockCopy(System.Text.Encoding.Default.GetBytes(shortFileName), 0, buffer, (len_AesKey + len_AesIV), shortFileName.Length); // 8*25

                // .3 Encrypt buffer
                // . 3.1 define private key file
                RSACryptoServiceProvider rsaProver;
                if (!selectedPersonalKey)
                {
                    //keep .data and .key files has the same file name.
                    saveFileName = getRealSaveFileName(Path.Combine(currentPath, shortFileName + ".temp_key" + encryptedFileExt));
                    string rsaPrivateKeyFile = saveFileName.Substring(0, saveFileName.Length - encryptedFileExt.Length) + encryptedkeyExt;

                    // . 3.2 Generate RSA key and save.
                    Models.RsaHelper.GenerateRsaKeyPair(rsaPrivateKeyFile);
                    rsaProver = Models.RsaHelper.PrivateKeyFromPemFile(rsaPrivateKeyFile);
                }
                else
                {
                    saveFileName = getRealSaveFileName(Path.Combine(currentPath, shortFileName + ".personal_key" + encryptedFileExt));
                    string rsaPublicKeyFile = Path.Combine(Directory.GetCurrentDirectory(), "personal_rsa2048_public" + encryptedkeyExt);
                    try
                    {
                        rsaProver = Models.RsaHelper.PublicKeyFromPemFile(rsaPublicKeyFile);
                    }
                    catch
                    {
                        if (_gLanguageIsEnglish)
                            MessageBox.Show("personal_rsa2048_public.key is not a RSA public key.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        else
                            MessageBox.Show("personal_rsa2048_public.key 不是正确的公钥文件。", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                        isCompleted = -1;
                        return;
                    }
                }
                // . 3.2 encrypt by using RSA key.
                byte[] encryptedKeyIV = rsaProver.Encrypt(buffer, RSAEncryptionPadding.Pkcs1);

                //Create encryption file
                // .1 Write file HEAD.
                using (var file = File.Create(saveFileName))
                {
                    file.Write(System.Text.Encoding.ASCII.GetBytes(str_HeaderFlag + str_HeaderVersion)); //8 Bytes;
                    file.Write(encryptedKeyIV); //256 Bytes;
                }

                // .2 Write data.
                using (FileStream outFile = new FileStream(saveFileName, FileMode.OpenOrCreate, FileAccess.Write))
                {
                    //skip header to dada.
                    outFile.Seek(len_Header + len_DecryptedString, SeekOrigin.Begin);

                    using (CryptoStream encryptoStream = new CryptoStream(outFile, encAlg.CreateEncryptor(), CryptoStreamMode.Write))
                    {
                        using (FileStream inFile = new FileStream(fileToEncrypt, FileMode.Open, FileAccess.Read))
                        {
                            inFile.CopyTo(encryptoStream);
                        }
                    }
                }
            }
            else if (iAction == ISDecrypt)   //decrypt.
            {
                //Get AES256 key. 
                var decAlg = Aes.Create();
                // .1 Get decrypted KEY and IV.
                byte[] buffer = new byte[len_DecryptedString];
                using (var file = File.OpenRead(fileToDecrypt[0]))
                {
                    file.Seek(len_Header, SeekOrigin.Begin);
                    file.Read(buffer, 0, len_DecryptedString);
                }
                // .2 Decrypt KEY and IV.
                byte[] decryptData;
                try
                {
                    RSACryptoServiceProvider rsaProver = Models.RsaHelper.PrivateKeyFromPemFile(fileToDecrypt[1]);
                    decryptData = rsaProver.Decrypt(buffer, RSAEncryptionPadding.Pkcs1);
                }
                catch (Exception)
                {
                    if (_gLanguageIsEnglish)
                        MessageBox.Show("Decrypt failed: The key is wrong, please use the right private key.(-100)", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    else
                        MessageBox.Show("解密失败：私钥不正确，请使用正确的私钥。(-100)", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                    isCompleted = -1;
                    return;
                }
                var tmpKey = new byte[len_AesKey];
                Buffer.BlockCopy(decryptData, 0, tmpKey, 0, len_AesKey);
                decAlg.Key = tmpKey;
                var tmpIV = new byte[len_AesIV];
                Buffer.BlockCopy(decryptData, len_AesKey, tmpIV, 0, len_AesIV);
                decAlg.IV = tmpIV;
                var tmpFileName = new byte[len_MaxFileName];
                Buffer.BlockCopy(decryptData, len_AesKey + len_AesIV, tmpFileName, 0, len_MaxFileName);
                saveFileName = System.Text.Encoding.Default.GetString(tmpFileName);
                saveFileName = saveFileName.Substring(0, saveFileName.IndexOf('\0'));
                // option: [check for crack]
                if (true)
                {
                    if (saveFileName.LastIndexOf('/') > 0
                        || saveFileName.LastIndexOf('\\') > 0
                        || saveFileName.LastIndexOf(':') > 0
                        || saveFileName.LastIndexOf('*') > 0
                        || saveFileName.LastIndexOf('?') > 0
                        || saveFileName.LastIndexOf('"') > 0
                        || saveFileName.LastIndexOf('<') > 0
                        || saveFileName.LastIndexOf('>') > 0
                        || saveFileName.LastIndexOf('|') > 0
                        )
                    {
                        if (_gLanguageIsEnglish)
                            MessageBox.Show("Decrypt failed: The encrypted file is wrong, please use the right encrypted file.(-200)", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        else
                            MessageBox.Show("解密失败: 加密文件不正确，请使用正确的加密文件。(-200)", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                        isCompleted = -1;
                        return;
                    }
                }

                //Create file stream
                // rename output file name.
                saveFileName = getRealSaveFileName(Path.Combine(currentPath, saveFileName));
                using (FileStream inFile = new FileStream(fileToDecrypt[0], FileMode.Open, FileAccess.Read))
                {
                    inFile.Seek(len_Header + len_DecryptedString, SeekOrigin.Begin);
                    using (CryptoStream decryptoStream = new CryptoStream(inFile, decAlg.CreateDecryptor(), CryptoStreamMode.Read))
                    {
                        using (FileStream outFile = new FileStream(saveFileName, FileMode.OpenOrCreate, FileAccess.Write))
                        {
                            try
                            {
                                decryptoStream.CopyTo(outFile);
                            }
                            catch (CryptographicException)
                            {
                                if (_gLanguageIsEnglish)
                                    MessageBox.Show("Decrypt failed: Maybe the encrypted file or the private key file is wrong.(-100)", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                                else
                                    MessageBox.Show("解密失败: 可能是加密文件或密钥文件不正确。(-100)", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                                isCompleted = -1;
                            }
                        }
                    }
                }
            }

            if (isCompleted == 0)
                isCompleted = 1;
        }

        static void Delay(int mm)
        {
            DateTime current = DateTime.Now;
            while (current.AddMilliseconds(mm) > DateTime.Now)
            {
                Thread.Sleep(10);
                System.Windows.Forms.Application.DoEvents();
            }
            return;
        }

        //Pinvoke for API function
        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool GetDiskFreeSpaceEx(string lpDirectoryName,
            out ulong lpFreeBytesAvailable,
            out ulong lpTotalNumberofBytes,
            out ulong lpTotalNumberOfFreeBytes);
        private static ulong DriveFreeBytes(string folderName)
        {
            if (string.IsNullOrEmpty(folderName))
                throw new ArgumentNullException("folderName");
            if (!folderName.EndsWith("\\"))
                folderName += '\\';

            ulong freeSpace = 0, dummy1 = 0, dummy2 = 0;
            if (!GetDiskFreeSpaceEx(folderName, out freeSpace, out dummy1, out dummy2))
                throw new Win32Exception(Marshal.GetLastWin32Error());

            return freeSpace;
        }
        private static bool HasWritePermissionOnDir(string path)
        {
            /* "Directory.GetAccessControl" is not implement in .NET 6, we have to wait.
            {
                var writeAllow = false;
                var writeDeny = false;
                var accessControlList = Directory.GetAccessControl(path);
                if (accessControlList == null)
                    return false;
                var accessRules = accessControlList.GetAccessRules(true, true, typeof(System.Security.Principal.SecurityIdentifier));
                if (accessRules == null)
                    return false;

                foreach (FileSystemAccessRule rule in accessRules)
                {
                    if ((FileSystemRights.Write & rule.FileSystemRights) != FileSystemRights.Write)
                        continue;

                    if (rule.AccessControlType == AccessControlType.Allow)
                        writeAllow = true;
                    else if (rule.AccessControlType != AccessControlType.Deny)
                        writeDeny = true;
                }

                return writeAllow && !writeDeny;
            }
            */

            bool success = false;
            string fullpath = Path.Combine(path, "._tmp_.tmp");

            if (Directory.Exists(path))
            {
                try
                {
                    using (FileStream fs = new FileStream(fullpath, FileMode.CreateNew, FileAccess.Write))
                    {
                        fs.WriteByte(0xff);
                    }

                    if (File.Exists(fullpath))
                    {
                        File.Delete(fullpath);
                        success = true;
                    }
                }
                catch (Exception)
                {
                    success = false;
                }
            }
            return success;
        }

        static bool selectedPersonalKey = false;
        static bool guiInited = false;
        //Switch mode 
        private void ComboBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            //Skip the default selected at boot.
            if (!guiInited)
            {
                guiInited = true;
                return;
            }

            if (comboxTemporary.IsSelected)
            {
                selectedPersonalKey = false;
                textKeyPath.Text = "";

                //change theme.
                SetTemporaryKeyTheme(true);
            }
            else
            {
                var publicPemFullFileName = Path.Combine(Directory.GetCurrentDirectory(), "personal_rsa2048_public.key");
                if (!File.Exists(publicPemFullFileName))
                {
                    //Generate RSA key or select a key exists.
                    var msg = "";
                    MessageBoxResult msgResult;
                    if (_gLanguageIsEnglish)
                    {
                        msg = "RSA public key not found." + Environment.NewLine +
                            "click \"Yes\" to generate RSA key pair,  or \"No\" to select a exists RSA public key.";
                        msgResult = MessageBox.Show(msg, "Please select", MessageBoxButton.YesNoCancel);
                    }
                    else
                    {
                        msg = "RSA公钥不存在。" + Environment.NewLine +
                            "点击\"是\"生成新的RSA密钥对，或点击\"否\"选择指定的RSA公钥。";
                        msgResult = MessageBox.Show(msg, "请选择", MessageBoxButton.YesNoCancel);
                    }

                    if (msgResult == MessageBoxResult.Yes)  //Generate RSA key pair.
                    {
                        Models.RsaHelper.GenerateRsaKeyPair(Path.Combine(Directory.GetCurrentDirectory(), "personal_rsa2048_private.key"), publicPemFullFileName);
                        if (_gLanguageIsEnglish)
                        {
                            msg = "RSA key pair generated." + Environment.NewLine + "\"personal_rsa2048_public.key\" for encrypt, and \"personal_rsa2048_private.key\" for decrypt." + Environment.NewLine + "Please keep personal_rsa2048_private.key safty.";
                            MessageBox.Show(msg, "Notice");
                        }
                        else
                        {
                            msg = "RSA密钥对己生成。" + Environment.NewLine + "公钥\"personal_rsa2048_public.key\"用于加密，私钥\"personal_rsa2048_private.key\"用于解密。" + Environment.NewLine + "请保管好您的私钥文件。";
                            MessageBox.Show(msg, "提示");
                        }
                        OpenExplorer(Path.Combine(Directory.GetCurrentDirectory(), "personal_rsa2048_public.key"));
                    }
                    else if (msgResult == MessageBoxResult.No)  //Select a exists RSA public key.
                    {
                        var dlg = new System.Windows.Forms.OpenFileDialog();
                        dlg.Filter = "RSA Public/Private Key (*.key)|*.key";
                        if (dlg.ShowDialog() != System.Windows.Forms.DialogResult.OK)
                        {
                            comboxTemporary.IsSelected = true;
                            return;
                        }
                        //Get public key even from private key.
                        Models.RsaHelper.PublicKeyCopyto(dlg.FileName, publicPemFullFileName);
                    }
                    else
                    {
                        comboxTemporary.IsSelected = true;
                        return;
                    }
                }
                textKeyPath.Text = Path.GetFileName(publicPemFullFileName);
                selectedPersonalKey = true;

                //Change theme.
                SetTemporaryKeyTheme(false);
            }
        }

        public void SetTemporaryKeyTheme(bool isTemporaryTheme)
        {
            if (isTemporaryTheme)
                btnEncryption.Background = new SolidColorBrush(Colors.Red);
            else
                btnEncryption.Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb(50, 11, 134)); //back to color(DeepPurple);
        }

        private void textKeyPath_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (textKeyPath.Text.Length > 0)
                OpenExplorer(Path.Combine(Directory.GetCurrentDirectory(), textKeyPath.Text));
        }

        private void Hyperlink_RequestNavigate(object sender, System.Windows.Navigation.RequestNavigateEventArgs e)
        {
            Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri) { UseShellExecute = true });
        }

        string GetSaveDir(string filename)
        {
            //Check for save space and access permission.
            var fi = new FileInfo(filename);
            long fileSize = fi.Length;

            string saveDir = Path.GetDirectoryName(filename);
        label_pathCheck:
            {
                if ((DriveFreeBytes(saveDir) < (ulong)fileSize) || !HasWritePermissionOnDir(saveDir))
                {
                    // Notice
                    if (_gLanguageIsEnglish)
                    {
                        string msg = saveDir + ": " + Environment.NewLine + "Directory space is too small to write or no write permission, please select a new directory.";
                        MessageBox.Show(msg, "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                    else
                    {
                        string msg = saveDir + ": " + Environment.NewLine + "源文件所在目录空间不足或没有写入权限，请选择一个可用的目录。";
                        MessageBox.Show(msg, "警告", MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                    //select save path.
                    var folderBrowser = new System.Windows.Forms.FolderBrowserDialog();
                    //folderBrowser.Description = "Select a folder to save...";
                    folderBrowser.SelectedPath = saveDir;
                    if (System.Windows.Forms.DialogResult.OK == folderBrowser.ShowDialog())
                    {
                        saveDir = folderBrowser.SelectedPath;
                        goto label_pathCheck;
                    }
                }
                return saveDir;
            }
        }

        int callAndWaitProcess(int iAction)
        {
            //<Thread VS Task>
            // <use Thread>
            var thread = new Thread(() => doProcess(iAction));
            thread.Name = "doProcess";
            thread.Start(); //parameter type has to be of 'Object' Type
            // <use Task>
            //Task task = Task.Factory.StartNew(() => doProcess());
            //Task.WaitAll(task)
            do
            {
                Delay(100);
            } while (isCompleted == 0);

            return isCompleted;
        }
        private void btnEncryt_Click(object sender, RoutedEventArgs e)
        {
            // No files
            if (fileToEncrypt.Length == 0)
            {
                if (_gLanguageIsEnglish)
                    MessageBox.Show("Please drag file first.", "Tips", MessageBoxButton.OK, MessageBoxImage.Information);
                else
                    MessageBox.Show("请先选择（拖放）一个文件。", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            // Check files.
            if (!File.Exists(fileToEncrypt))
            {
                if (_gLanguageIsEnglish)
                    MessageBox.Show(fileToEncrypt + " is not exists.", "Warning");
                else
                    MessageBox.Show(fileToEncrypt + " 文件不存在。", "警告");

                return;
            }
            if (selectedPersonalKey && !File.Exists(Path.Combine(Directory.GetCurrentDirectory(), "personal_rsa2048_public.key")))
            {
                if (_gLanguageIsEnglish)
                    MessageBox.Show("personal_rsa2048_public.key" + " is not exists.", "Warning");
                else
                    MessageBox.Show("personal_rsa2048_public.key" + " 文件不存在。", "警告");
                comboxTemporary.IsSelected = true;
                return;
            }

            //Show running gif
            en_waitingGif.Visibility = Visibility.Visible;
            btnEncryption.IsEnabled = false;

            currentPath = GetSaveDir(fileToEncrypt);

            if (callAndWaitProcess(ISEncrypt) == 1) //success.
                OpenExplorer(saveFileName);

            // clear GUI
            en_dropTips.Visibility = Visibility.Visible;
            en_imageIcon.Visibility = Visibility.Hidden;
            en_lab.Content = "";
            en_waitingGif.Visibility = Visibility.Hidden;
            btnEncryption.IsEnabled = true;

            fileToEncrypt = "";
        }
        private void btnDecrypt_Click(object sender, RoutedEventArgs e)
        {
            // No files
            if (fileToDecrypt[0].Length == 0 || fileToDecrypt[1].Length == 0)
            {
                if (_gLanguageIsEnglish)
                    MessageBox.Show("Please drag files first.", "Tips", MessageBoxButton.OK, MessageBoxImage.Information);
                else
                    MessageBox.Show("请先选择（拖放）文件。", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            // Check files.
            if (!File.Exists(fileToDecrypt[0]))
            {
                if (_gLanguageIsEnglish)
                    MessageBox.Show(fileToDecrypt[0] + " is not exists.", "Warning");
                else
                    MessageBox.Show(fileToDecrypt[0] + " 文件不存在。", "警告");
                return;
            }
            if (!File.Exists(fileToDecrypt[1]))
            {
                if (_gLanguageIsEnglish)
                    MessageBox.Show(fileToDecrypt[1] + " is not exists.", "Warning");
                else
                    MessageBox.Show(fileToDecrypt[1] + " 文件不存在。", "警告");
                return;
            }

            //Show running gif
            de_waitingGif.Visibility = Visibility.Visible;
            btnDecryption.IsEnabled = false;

            currentPath = GetSaveDir(fileToDecrypt[0]);

            if (callAndWaitProcess(ISDecrypt) == 1) //success.
                OpenExplorer(saveFileName);

            // clear GUI
            de_dropTips.Visibility = Visibility.Visible;
            de_imageIcon1.Visibility = Visibility.Hidden;
            de_lab1.Content = "";
            de_imageIcon2.Visibility = Visibility.Hidden;
            de_lab2.Content = "";
            de_waitingGif.Visibility = Visibility.Hidden;
            btnDecryption.IsEnabled = true;

            fileToDecrypt[0] = "";
            fileToDecrypt[1] = "";
        }

        private void SponsorLogo_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            OpenUrl("https://ancun.cloud");
        }

        private void Login_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            OpenUrl("https://ancun.cloud/tools.html");
        }

        public static void OpenUrl(string url)
        {
            if (string.IsNullOrEmpty(url))
                return;

            var psi = new ProcessStartInfo
            {
                FileName = url,
                UseShellExecute = true
            };
            Process.Start(psi);
        }

        public static void OpenExplorer(string fileFullPath)
        {
            if (string.IsNullOrEmpty(fileFullPath))
                return;

            string args = "/select,\"" + fileFullPath + "\"";
            Process.Start("explorer.exe", args);
        }

        private void SelectLanguage_Checked(object sender, RoutedEventArgs e)
        {
            _gLanguageIsEnglish = false;

            string requestedCulture = @"Resources\language\zh.xaml";
            int languageId = System.Globalization.CultureInfo.CurrentCulture.LCID;
            List<ResourceDictionary> dictionaryList = new List<ResourceDictionary>();
            foreach (ResourceDictionary dictionary in Application.Current.Resources.MergedDictionaries)
            {
                if (dictionary.Source != null)
                    dictionaryList.Add(dictionary);
            }
            ResourceDictionary resourceDictionary = dictionaryList.FirstOrDefault(d => d.Source.OriginalString.Equals(requestedCulture));
            Application.Current.Resources.MergedDictionaries.Remove(resourceDictionary);
            Application.Current.Resources.MergedDictionaries.Add(resourceDictionary);

            Keyboard.ClearFocus();
        }

        private void SelectLanguage_Unchecked(object sender, RoutedEventArgs e)
        {
            _gLanguageIsEnglish = true;

            string requestedCulture = @"Resources\language\en.xaml";
            int languageId = System.Globalization.CultureInfo.CurrentCulture.LCID;
            List<ResourceDictionary> dictionaryList = new List<ResourceDictionary>();
            foreach (ResourceDictionary dictionary in Application.Current.Resources.MergedDictionaries)
            {
                if (dictionary.Source != null)
                    dictionaryList.Add(dictionary);
            }
            ResourceDictionary resourceDictionary = dictionaryList.FirstOrDefault(d => d.Source.OriginalString.Equals(requestedCulture));
            Application.Current.Resources.MergedDictionaries.Remove(resourceDictionary);
            Application.Current.Resources.MergedDictionaries.Add(resourceDictionary);

            Keyboard.ClearFocus();
        }
    }
}

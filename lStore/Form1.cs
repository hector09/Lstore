﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Net;
using System.Net.Sockets;
using System.Net.NetworkInformation;
using System.IO;
using System.Xml;
using System.Threading;
using System.Collections;
using System.Web;
//=========================================namespaces till here==============
namespace lStore
{
    public partial class lStore : Form
    {
        //=====variables here===================
        public string userName = Environment.UserName, localName;
        public string primaryFolder;
        public string ip, baseaddr, gatewayIPv4, gatewayIPv6;
        public string randomFileName;   //a random file name for a file which stores temporary dat about the xml
        public ArrayList onlineUser = new ArrayList();      //for stroring name of online user's name to be populated from db
        public ArrayList onlineUserIp = new ArrayList();
        public ArrayList tmpArrayList = new ArrayList();    //for storing tmp online user's while loading the online user's list
        public int onlineUsercount = 0, tmpOnlineUserCount = 0, loadercount = 0;
        public lStore()
        {
            InitializeComponent();
            localName = System.Environment.MachineName;
            primaryFolder = @"C:\Users\" + userName + @"\Documents\lStore";
            if (!isInternetConnected()) 
            { 
                internetState.Text = "No internet connection";
                internetState.ForeColor = System.Drawing.Color.Red;
            }
            else { 
                internetState.Text = "Connected to internet";
                internetState.ForeColor = System.Drawing.Color.Green;
                  }
            saveUsage();    //stores the usage date and time to file
            //getGatewayDetails();    //this get gateway details from system
            gatewayIPv4 = "192.168.100.1";
            try
            {
                baseaddr = getBaseAddress(gatewayIPv4);
            }
            catch (NullReferenceException ex)
            {
                throwNonRecoverableError(ex.Message);
                Environment.Exit(0);    //this has to be gradually changed to something more reliable
            }
            getIpAddress(); 
        }
        private void Form1_Load(object sender, EventArgs e)
        {
            /*task here is to check if this is first time or not
            this is the first task to be performed by the tool in main thread
             */
            if (isFirstTime())
            {
                //this means this is its first time
                MessageBox.Show(" Dear user, this is the first time you are using this app!\n We will now register you to our server so that you can use the app without any flaws. \nThanks for downloading this software!!");
                //save data to save.ini in lStore folder
                //create cache folder
                getSystemDetails(); //this get us the required details
                //Thread th = new Thread(saveXML);
                //th.Start();
                saveXML();
                //update xml to server
            }
            else 
            {
                //this means this is not the first time
                //task -> get required details
                //update and match xml from server and take required actions
            }
            //tier two task is to :
            /*
             * 1: scan in alternate thread
             * 2: match the xml details against that in server, another thread 
             * 3: check the db against last scan date/time and scan if it exceeds limit time -> upload to server
             * 
             */
              //task here is to load username,network name, files shared and rating to UI
            uname.Text = "" +userName;
            nname.Text = @"\\" +localName;
            rating.Text = returnRating();
            codeLocation.Text = returnLocation();
            countFilesShared.Text = returnCountFileShared();
            /*
             * code now to populate list with online users and then trigger a function to recheck online users
             */
            populateUserList();
            Thread th = new Thread(gatherOnlineUser);
            th.Start();
            

        }
        /* this function calls the member of another class in another thread 
         * so that it can retireve list of online users on LAN
         */ 
        public void gatherOnlineUser() {
            users userObj = new users(baseaddr,primaryFolder);

        }
        /*
         * this function retrieve the user list from file and and populate it to list
         */
        public void populateUserList()
        {
            /* need to add code to get IP from file as well */
            onlineUser.Clear();
            onlineUsers.Items.Clear();
            /*
             * cross thread operations result in exception so you need to check weather an invoke is required prior to you using 
             * this */
            try
            {
                string[] tmp = File.ReadAllLines(primaryFolder + @"\tmp\online.data");
                onlineUsercount = tmp.Length;
                countOnline.Text = "( " + onlineUsercount + " )";
                for (int i = 0; i < onlineUsercount; i++)
                {
                    onlineUser.Add(tmp[i]);
                    onlineUsers.Items.Add(tmp[i]);
                }
                //code to populate this to the list view
            }
            catch (Exception ex)
            {
                saveException(ex.Message);
            }
            
        }
        public void saveXML()
        {
            //primaryFolder = @"C:\Users\" + userName + @"\Documents\lStore";
            string xml  = "<?xml version=\"1.0\" encoding=\"UTF-8\"?>" +Environment.NewLine;
            xml += "<data>" + Environment.NewLine;
            xml += "<username>" + userName + "</username>" + Environment.NewLine;
            xml += "<localname>" + localName + "</localname>" + Environment.NewLine;
            xml += "<rating>0.0</rating>" + Environment.NewLine;
            try
            {
                File.WriteAllText(primaryFolder + @"\savedfile.xml", xml);
            }
            catch (Exception ex)
            {
                saveException(ex.Message);
                MessageBox.Show("We are unable to save details to saved file! Program exits here! " +ex.Message);
                this.Close();
            }
        }
        /* 
         * a function to read xml file and retrieve the rating from it
         * and save rating = 0 and return rating in case rating does not exists
         * xml parsing is not working
         */
        public string returnRating()
        {
            string xmlData;
            try
            {
                xmlData = File.ReadAllText(primaryFolder + @"\savedfile.xml");
            }
            catch (FileLoadException ex)
            {
                saveException(ex.Message);
                repairFiles();
                xmlData = File.ReadAllText(primaryFolder + @"\savedfile.xml");
            }
            using (XmlReader reader = XmlReader.Create(new StringReader(xmlData + "</data>")))
            {
                    reader.ReadToFollowing("rating");
                    reader.MoveToFirstAttribute();
                    string data = reader.Value;
                    if (data.Length == 0)
                    {
                        //saveXML();
                        /* 
                         * need to find an alternative
                         */ 
                        return "0.0";
                    }
                    return data;
            }
        }
        /* 
         * a fucntion to read location code from xml file and return it 
         * incase of missing data request server for the same 
         * and save data to xml
         * alternative: request xml data from adjoining system-> connected to same LAN
         */
        public string returnLocation()
        {
            string xmlData;
            try
            {
                xmlData = File.ReadAllText(primaryFolder + @"\savedfile.xml");
            }
            catch (FileLoadException ex)
            {
                saveException(ex.Message);
                repairFiles();
                xmlData = File.ReadAllText(primaryFolder + @"\savedfile.xml");
            }
            using (XmlReader reader = XmlReader.Create(new StringReader(xmlData + "</data>")))
            {
                reader.ReadToFollowing("location");
                reader.MoveToFirstAttribute();
                string data = reader.Value;
                if (data.Length == 0)
                {
                    /*
                     * function to send default gateway address to server and retrieve the location code
                     * and then function to save this data to xml
                     */ 
                    return "loading..";
                }
                return data;
            }
        }
        /*
         * a function to return the no of files shared by user from XML
         * if not available in XML call the function that tracks the no of shared files
         */
        public string returnCountFileShared()
        {
            string xmlData;
            try
            {
                xmlData = File.ReadAllText(primaryFolder + @"\savedfile.xml");
            }
            catch (FileLoadException ex)
            {
                saveException(ex.Message);
                repairFiles();
                xmlData = File.ReadAllText(primaryFolder + @"\savedfile.xml");
            }
            using (XmlReader reader = XmlReader.Create(new StringReader(xmlData + "</data>")))
            {
                reader.ReadToFollowing("filescount");
                reader.MoveToFirstAttribute();
                string data = reader.Value;
                if (data.Length == 0)
                {
                    /*
                     *function call to calculate no of files shared by user 
                     */
                    return "scanning..";
                }
                return data;
            }
        }
        /*
         * this function generates a random 8 digit string and returns to you
         */
        string returnRandom()
        {
            string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            Random random = new Random();
            string result = new string(
                Enumerable.Repeat(chars, 8)
                          .Select(s => s[random.Next(s.Length)])
                          .ToArray());
            return result;
        }
        /*
         * this function get details about the system from system an saves it to the variables
         */
        public void getSystemDetails()
        { 
            //get unique id
            //need of this function need to be thought off again
        }
        /* this function checks for each required folders if they exist or not and create the required folder 
         * according to need
         * params:no
         * dir: C:\Users\<userName>\Documents\lStore
         * folders: ...lStore\, ...lStore\tmp
         */
        public void repairFolders()
        {
            string mainFolder = @"C:\Users\" + userName + @"\Documents\lStore";
            if (!Directory.Exists(mainFolder)) { Directory.CreateDirectory(mainFolder); }
            string tmpFolder = mainFolder + @"\tmp";
            if (!Directory.Exists(tmpFolder)) { Directory.CreateDirectory(tmpFolder); }
        }
        /*
         a function to check if al required files exist and create necessory one when needed
         * params: no
         * files: saved.xml, usage.log, search: log,exceptions.log
         * dir: C:\Users\<userName>\Documents\lStore
         */
        public void repairFiles()
        {
            
            if (!File.Exists(primaryFolder + @"\savedfile.xml"))
            {
                saveXML();
            }
            if (!File.Exists(primaryFolder + @"\usage.log")){File.Create(primaryFolder + @"\usage.log");}
            if (!File.Exists(primaryFolder + @"\search.log")) { File.Create(primaryFolder + @"\search.log"); }
            if (!File.Exists(primaryFolder + @"\exceptions.log")) { File.Create(primaryFolder + @"\exceptions.log"); }
            if (!File.Exists(primaryFolder + @"\tmp\online.data")) { File.Create(primaryFolder + @"\tmp\online.data"); }
        }
        /* 
         * a function to get the gateway address in IPv6 and IPv4 form
         */ 
        public void getGatewayDetails()
        {
            int count = 0;
            foreach (NetworkInterface f in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (f.OperationalStatus == OperationalStatus.Up)
                {
                    foreach (GatewayIPAddressInformation d in f.GetIPProperties().GatewayAddresses)
                    {
                        if (count == 0) { gatewayIPv6 = d.Address.ToString(); count++; }
                        else { gatewayIPv4 = d.Address.ToString(); }
                        //alternate method needed
                    }
                }
            }
        }
        /*
         * a function to get base address out of default parameter ip
         * @param: string ip: any ip address
         */ 
        public string getBaseAddress(string ip)
        {
            string[] parts = ip.Split('.');
            return parts[0] + '.' + parts[1] + '.' + parts[2]; 
        }
        /*
         * function to get ip address of the system
         */
        public void getIpAddress()
        {
            if (baseaddr.Length < 6)
            {
                //we assume this base address has not been already taken
                getGatewayDetails();
                baseaddr = getBaseAddress(gatewayIPv4);
            }
            string hostName = Dns.GetHostName();
            IPHostEntry myself = Dns.GetHostByName(hostName);
            foreach (IPAddress address in myself.AddressList)
            {
                if (getBaseAddress(address.ToString()) == baseaddr)
                {
                    ip = address.ToString();
                }
            }
        }
        /*
         * this function checks if this is first time user is using this app
         * parameters: null
         * return type: bool
         */
        public bool isFirstTime()
        { 
            /*
             * check for the saved file if it does not exists this is first time
             */
            primaryFolder = @"C:\Users\" +userName +@"\Documents\lStore";
            if (Directory.Exists(primaryFolder))
            {
                if (File.Exists(primaryFolder + @"\savedfile.xml"))
                {
                    return false;
                }
            }
            else {
                Directory.CreateDirectory(primaryFolder);
                //now the directory is created
            }
            return true;
        }
        /* 
         * returns true or false corresponding to working internet connection
         */ 
        public bool isInternetConnected()
        {
            return NetworkInterface.GetIsNetworkAvailable();
        }
       
       /*
        * program to change profile image
        * this function will be called when the user clicks on the profilepic
        * it should lead to a function that allows to change the profile picture
        * need to learn about this
        * parameters: null
        * return type: void
        * thread: main thread
        */
       private void linkChangeImage_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
       {
           var FD = new System.Windows.Forms.OpenFileDialog();
           if (FD.ShowDialog() == System.Windows.Forms.DialogResult.OK)
           {
               string fileToOpen = FD.FileName;
               string[] splitname = fileToOpen.Split('.');
               string extension = splitname[(splitname.Length - 1)].ToLower();
               string profileImageDirec = @"C:\Users\" + userName + @"\Documents\lStore\tmp\user." + extension;
               if (extension == "jpg" || extension == "png" || extension == "bmp" || extension == "jpeg")
               {
                   //System.IO.FileInfo File = new System.IO.FileInfo(FD.FileName);
                   //System.IO.StreamReader reader = new System.IO.StreamReader(fileToOpen);
                   if (File.Exists(profileImageDirec)) File.Delete(profileImageDirec);
                   try
                   {
                       File.Copy(fileToOpen, profileImageDirec);
                   }
                   catch (DirectoryNotFoundException ex)
                   {
                       repairFolders();
                       File.Copy(fileToOpen, profileImageDirec);
                   }
                   catch (Exception ex)
                   {
                       MessageBox.Show(ex.Message); 
                        //more actions need to be added to counter a situation like this
                   }
                   //====task is to resize image in  tmp folder now=====
                   /*
                    * process: resize img in tmp -> delete user.xt in lstore@/ and 
                    * move lstore/tmp/user.xt to lstore/user.xt
                    * task done
                    * a loader till this task is finished
                    */ 
                   //after this reload the image pane inmain UI
               }
               else
               {
                   MessageBox.Show("Invalid file format! file should be \"jpg\", \"jpeg\", \"png\" or \"bmp\" ");
               }
           }
       }
        /* this function is responsible to initiate search */
       private void submitSearch_Click(object sender, EventArgs e)
       {
           string key = search.Text;
           if (key == "  Search here..." || key.Length == 0)
           {
               tmpLog.Text = "Enter Something first!";
               search.Focus();
           }
           else
           {    
               /*
                case when something logical has been attempted
                */
               tmpLog.Text = "Searching for \" " + key + " \"...";
               //save this search to log
               writeToSearchLogs(key);
               
           }
       }
       /*
        * this function saves each search to a search log with date and time
        * filename: search.log
        * dir: C:\Users\<userName>\Documents\lStore
        * @param: string log: ie the search key
        */
       public void writeToSearchLogs(string log)
       {
           try
           {
               primaryFolder = @"C:\Users\" + userName + @"\Documents\lStore";
               DateTime now = DateTime.Now;
               File.AppendAllText(primaryFolder + @"\search.log", log + "||" + now + Environment.NewLine);
           }
           catch (DirectoryNotFoundException ex)
           {
               repairFolders();
               writeToSearchLogs(log);
               saveException(ex.Message);
           }
           catch (Exception ex)
           {
               throwNonRecoverableError(ex.Message);
           }
       }
       /*
        * this function saves each time this application is opened to a log file 
        * filename: usage.log
        * dir: C:\Users\<userName>\Documents\lStore
        * no params
        */
       public void saveUsage()
       {
           try
           {
               primaryFolder = @"C:\Users\" + userName + @"\Documents\lStore";
               DateTime now = DateTime.Now;
               File.AppendAllText(primaryFolder + @"\usage.log", now + Environment.NewLine);
           }
           catch (DirectoryNotFoundException ex)
           {
               repairFolders();
               saveUsage();         //exception handeling
               saveException(ex.Message);
           }
           catch (Exception ex)
           {
               throwNonRecoverableError(ex.Message);
           }
       }
       /*
        * a function to save exception to a log file!!
        * params: string ex: the exception message
        * filename: exceptions.log
        * dir: C:\Users\<userName>\Documents\lStore
        */
       public void saveException(string ex)
       {
           try
           {
               primaryFolder = @"C:\Users\" + userName + @"\Documents\lStore";
               DateTime now = DateTime.Now;
               File.AppendAllText(primaryFolder + @"\exceptions.log", ex + "||" + now + Environment.NewLine);
           }
           catch (DirectoryNotFoundException exep)
           {
               repairFolders();
               saveException(ex);
               saveException(exep.Message);
           }
           catch (Exception exep)
           {
               MessageBox.Show("Some error occured: " + exep.Message);
           }
       }
        /*
         * a function to handle non defined exceptions to be added later into account
         * save the exception into log and display the messageBox
         * @param: string err: ie the error message
         */
       public void throwNonRecoverableError(string err)
       {
           saveException(err);
           MessageBox.Show("Some error occured: " +err);
       } 
       private void search_TextChanged(object sender, EventArgs e)
       {
           /*
            codes to be implemented here to make a UI like google ajax call 
            * search suggestions
            */
       }
       private void filterUser_Click(object sender, EventArgs e)
       {
           filterUser.Focus();
       }
        /*
         * when ever user write some data to user filter this code refreshes the list
         */ 
       private void filterUser_TextChanged(object sender, EventArgs e)
       {
           onlineUsers.Items.Clear();
           string searchkey = filterUser.Text.ToLower();
           if (searchkey.Length == 0 || searchkey == "search....") 
           {
               foreach (string name in onlineUser)
               {
                   onlineUsers.Items.Add(name);
               }
           }
           
           foreach(string name in onlineUser)
           {
               if (name.ToLower().IndexOf(searchkey) != -1)
               {
                   onlineUsers.Items.Add(name);
               }
           }
       }

      

    }
}

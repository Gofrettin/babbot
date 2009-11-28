﻿/*
    This file is part of BabBot.

    BabBot is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    BabBot is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with BabBot.  If not, see <http://www.gnu.org/licenses/>.
  
    Copyright 2009 BabBot Team
*/
using System;
using System.ComponentModel;
using System.Diagnostics;
using BabBot.Bot;
using BabBot.Common;
using BabBot.Scripting;
using BabBot.Wow;
using Magic;
using System.Collections;
using System.Collections.Generic;
using Pather.Graph;
using System.Linq;
using System.Threading;
using System.IO;
using System.Xml.Serialization;
using System.Net;

namespace BabBot.Manager
{
    /// <summary>
    /// Main class for reading, writing and gathering process information 
    /// </summary>
    public class ProcessManager
    {
        #region Delegates

        /// <summary>
        /// Update notification on player fields
        /// </summary>
        public delegate void PlayerUpdateEventHandler();

        public delegate void PlayerWayPointEventHandler(Vector3D waypoint);

        /// <summary>
        /// Error is the Win32Exception.Message thrown.
        /// </summary>
        public delegate void WoWProcessAccessFailedEventHandler(string error);

        /// <summary>
        /// Process is the ID of the process that exited.
        /// </summary>
        public delegate void WoWProcessEndedEventHandler(int process);

        /// <summary>
        /// Error is the Win32Exception.Message thrown.
        /// </summary>
        public delegate void WoWProcessFailedEventHandler(string error);

        /// <summary>
        /// Process is the ID of the process that started.
        /// </summary>
        public delegate void WoWProcessStartedEventHandler(int process);

        /// <summary>
        /// Updates the status bar
        /// </summary>
        /// <param name="iStatus">what to write in the statusbar</param>
        public delegate void UpdateStatusEventHandler(string iStatus);

        /// <summary>
        /// First time run event
        /// </summary>
        public delegate void FirstTimeRunHandler();

        /// <summary>
        /// Configuration file change event
        /// </summary>
        public delegate void ConfigFileChangedHandler();

        /// <summary>
        /// Show Error Message
        /// </summary>
        /// <param name="err"></param>
        public delegate void ShowErrorMessageHandler(string err);

        #endregion

        #region WOWApplication Events

        /// <summary>
        /// ProcessFailed is fired if an exception is thrown when attempting to start the
        ///  process.
        /// </summary>
        public static event WoWProcessFailedEventHandler WoWProcessFailed;

        /// <summary>
        /// ProcessEnded is fired where the started process exits.
        /// </summary>
        public static event WoWProcessEndedEventHandler WoWProcessEnded;

        /// <summary>
        /// ProcessStarted is fired if the process successfully starts.
        /// </summary>
        public static event WoWProcessStartedEventHandler WoWProcessStarted;

        /// <summary>
        /// ProcessAccessFailed is fired if the current user does not have permission to
        ///  access the new process.
        /// </summary>
        public static event WoWProcessAccessFailedEventHandler WoWProcessAccessFailed;

        /// <summary>
        /// Fired when player Entered to WoW world
        /// </summary>
        public static event PlayerUpdateEventHandler WoWInGame;

        public static event PlayerUpdateEventHandler PlayerUpdate;
        public static event PlayerWayPointEventHandler PlayerWayPoint;

        public static event UpdateStatusEventHandler UpdateStatus;

        public static event FirstTimeRunHandler FirstTimeRun;
        public static event ConfigFileChangedHandler ConfigFileChanged;
        public static event ShowErrorMessageHandler ShowErrorMessage;

        #endregion

        #region Player Events

        #endregion

        #region Public Properties

        private static readonly BlackMagic wowProcess;
        public static BotManager BotManager;
        public static Caronte.Caronte Caronte;
        public static CommandManager CommandManager;
        public static InjectionManager Injector;
        private static Config config;
        private static WoWData wdata;
        private static WoWVersion wversion;
        public static ObjectManager ObjectManager;
        public static WowPlayer Player;
        private static Process process;
        public static Profile Profile;
        public static Host ScriptHost;
        public static uint TLS;
        public static int WowHWND;
        private static bool arun = false;
        // Config file name
        private static string ConfigFileName = "config.xml";
        // Last version of used config file
        private static int ConfigVersion = 2;

        public static WoWVersion CurWoWVersion
        {
            get { return wversion; }
        }

        public enum ProcessStatuses : byte
        {
            IDLE = 0,
            STARTING = 1,
            RUNNING = 2,
            CLOSED = 3,
            CRUSHED = 255,
        }

        public enum GameStatuses : byte
        {
            INIT = 0,
            TLS = 1,
            LOGGING = 2,
            IN_WORLD = 3,
            INITIALIZED = 4,
            DISCONNECTED = 255,
        }

        private static GameStatuses _gstatus;
        private static ProcessStatuses _pstatus;

        static ProcessManager()
        {
            config = new Config();
            wowProcess = new BlackMagic();
            _pstatus = ProcessStatuses.IDLE;
            _gstatus = GameStatuses.INIT;
            CommandManager = new CommandManager();
            Injector = new InjectionManager();
            TLS = 0x0;
            Profile = new Profile();
            WowHWND = 0;
            BotManager = new BotManager();
            ScriptHost = new Host();
            WayPointManager.Instance.Init();
            Caronte = new Caronte.Caronte();
        }

        /// <summary>
        /// Get the object for the manipulation of memory and general "hacking"
        /// of another process
        /// </summary>
        public static BlackMagic WowProcess
        {
            get
            {
                return wowProcess;
            }
        }

        /// <summary>
        /// Get the basic configuration for babBot: deprecated
        /// </summary>
        public static Config Config
        {
            get { return config; }
            set { config = value; }
        }

        public static WoWVersion[] WoWVersions
        {
            get { return (wdata != null) ? wdata.Versions : null; }
        }

        public static ArrayList TalentTemplateList
        {
            get
            {
                string[] dir;
                ArrayList res = new ArrayList(); ;

                // Scan Profiles/Talents for list
                string wdir = config.ProfilesDir +
                        System.IO.Path.DirectorySeparatorChar + "Talents";
                try
                {
                    dir = Directory.GetFiles(wdir, "*.xml");
                }
                catch (System.IO.DirectoryNotFoundException)
                {
                    if (WoWProcessFailed != null)
                    ShowErrorMessage("Directory '" + wdir + "' not found");
                    return res;
                }

                // Check each file
                foreach (string fname in dir)
                {
                    try
                    {
                        Talents tlist = ProcessManager.ReadTalents(fname);
                        if ((tlist != null) && (tlist.Description != null))
                            res.Add(tlist);
                    }
                    catch
                    {
                        // Continue 
                    }
                }

                return res;
            }
        }

        public static bool InGame
        {
            get { return (_gstatus == GameStatuses.INITIALIZED); }
        }

        #endregion

        #region Private Methods

        private static void afterProcessStart()
        {
            Debug("char", "Executing AfterStart ...");

            if (WoWProcessStarted != null)
                WoWProcessStarted(process.Id);

            try
            {
                _pstatus = ProcessStatuses.STARTING;

                // Set before using any BlackMagic methods
                process.EnableRaisingEvents = true;
                process.Exited += exitProcess;

                WowHWND = AppHelper.WaitForWowWindow();
                CommandManager.WowHWND = WowHWND;

                //verify we haven't already opened it, like when we do the injection
                if ((!wowProcess.IsProcessOpen &&
                        wowProcess.OpenProcessAndThread(process.Id)))
                {
                    // We don't let anyone do anything until wow has finished launching
                    // We look for the TLS first
                    _gstatus = GameStatuses.INIT;
                    _pstatus = ProcessStatuses.RUNNING;
                }

                while (!FindTLS())
                {
                   if (UpdateStatus != null)
                   {
                       UpdateStatus("Looking for the TLS ...");
                   }
                   Thread.Sleep(250);
                }

                if (config.WowPos != null)
                    BabBot.Common.WindowSize.SetPositionSize((IntPtr)WowHWND,
                        config.WowPos.Pos.X, config.WowPos.Pos.Y,
                        config.WowPos.Pos.Width, config.WowPos.Pos.Height);
                else if (config.WoWInfo.Resize)
                    BabBot.Common.WindowSize.SetPositionSize(
                                    (IntPtr)WowHWND, 0, 0, 328, 274);


                // At this point it should be safe to do any LUA calls
                if (config.WoWInfo.AutoLogin)
                {
                    Injector.Lua_RegisterInputHandler();
                    try
                    {
                        Login.AutoLogin(config.Account.RealmLocation, config.Account.GameType,
                                config.Account.Realm, config.Account.LoginUsername,
                                    config.Account.getAutoLoginPassword(), config.Character, 5);

                    }
                    catch (Exception e)
                    {
                        // Login exception means new patch
                        // Exit bot
                        ShowError(e.Message);
                        Environment.Exit(5);
                    }
                    Injector.Lua_UnRegisterInputHandler();
                }

                Debug("char", "AfterStart completed.");
            }
            catch (Exception e)
            {
                if (WoWProcessAccessFailed != null)
                {
                    WoWProcessAccessFailed(e.Message);
                }
            }
        }

        private static void exitProcess(object sender, EventArgs e)
        {
            // Do it first
            _pstatus = ProcessStatuses.CLOSED;
            _gstatus = GameStatuses.INIT;

            // Blah blah blah after
            Log("char", "WoW termination detected");
            Debug("char", "Executing After WoW termination ...");

            // Cleaning
            WowHWND = 0;
            Injector.IsLuaRegistered = false;
            Injector.ClearLuaInjection();
            wowProcess.CloseProcess();

            // TODO Check for crush

            if (WoWProcessEnded != null)
            {
                WoWProcessEnded(((Process) sender).Id);
            }

            Debug("char", "WoW termination completed");
        }

        private static void Log(string facility, string msg)
        {
            Output.Instance.Log(facility, msg);
        }

        private static void Debug(string facility, string msg)
        {
            Output.Instance.Debug(facility, msg);
        }

        /// <summary>
        /// Read URL and return result. Saved for future use
        /// </summary>
        /// <param name="url">URL to retrieve data from</param>
        /// <returns>HTTP response</returns>
        private string ReadURL(string url)
        {
            // Create a request for the URL.         
            WebRequest request = WebRequest.Create(url);
            // If required by the server, set the credentials.
            request.Credentials = CredentialCache.DefaultCredentials;
            // Get the response.
            HttpWebResponse response = (HttpWebResponse)request.GetResponse();
            // Display the status.
            // response.StatusDescription;
            // Get the stream containing content returned by the server.
            Stream stream = response.GetResponseStream();
            // Open the stream using a StreamReader for easy access.
            StreamReader reader = new StreamReader(stream);
            // Read the content.
            string res = reader.ReadToEnd();
            // Cleanup the streams and the response.
            reader.Close();
            stream.Close();
            response.Close();

            return res;
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Redirect error message to ShowErrorMessage handler
        /// </summary>
        /// <param name="err"></param>
        /// <returns></returns>
        public static bool ShowError(string err)
        {
            bool res = (ShowErrorMessage != null);
            if (res)
                ShowErrorMessage(err);
            return res;
        }

        /// <summary>
        /// Load content of WoWData.xml
        /// </summary>
        public static void LoadWowData()
        {
            XmlSerializer s = new XmlSerializer(typeof(WoWData));
            TextReader r = new StreamReader("WoWData.xml");

            try
            {
                wdata = (WoWData)s.Deserialize(r);
            }
            catch (Exception e)
            {
                ShowError("Unable load WoWData.xml : " + e.Message);
                Environment.Exit(1);
            }
            finally
            {
                r.Close();
            }
        }

        /// <summary>
        /// Start Bot Thread
        /// </summary>
        public static void StartBot()
        {
            // Reset process state so bot will pick it up
            _pstatus = ProcessStatuses.IDLE;

            // Start bot thread
            ProcessManager.BotManager.Start();
        }

        /// <summary>
        /// Try to run the WoW process 
        /// </summary>
        public static void StartWow()
        {
            _pstatus = ProcessStatuses.STARTING;

            try
            {
                // Perry style = paranoid ;-)
                string wowPath = Config.WoWInfo.ExePath;

                // Ok, no one configured the path, let's try to find it on our own
                if (string.IsNullOrEmpty(wowPath))
                {
                    wowPath = AppHelper.GetWowInstallationPath();
                }

                if (!string.IsNullOrEmpty(wowPath))
                {
                    Log("char", "Checking WoW version ...");
                    // Get WoW.exe version and convert to form x.x.x.x
                    string version = 
                        FileVersionInfo.GetVersionInfo(wowPath).FileVersion.
                            Replace(",", ".").Replace(" ", ""); ;

                    if (!wversion.ToString().Equals(version))
                    {
                        if (!ShowError("Version of WoW.exe '" + version + 
                                "' is not equal version from config.xml '" + wversion + "'"))
                            Environment.Exit(4);

                        return;
                    } else
                        Log("char", "Continuing with WoW.exe version '" + version + "'");
                        
                    Log("char", "Starting WoW ...");

                    // Guest account might not be enabled
                    try
                    {
                        // Process startup options
                        if (config.WoWInfo.NoSound)
                            wowPath += " -nosound";
                        if (config.WoWInfo.Windowed)
                            wowPath += " -windowed";

                        process = AppHelper.RunAs(Config.WoWInfo.GuestUsername,
                                            Config.WoWInfo.GuestPassword, null, wowPath);
                    }
                    catch (Win32Exception w32e)
                    {
                        if (WoWProcessFailed != null)
                        {
                            WoWProcessFailed("Unable to start '" + wowPath + 
                              "' with the Guest account (" + w32e.Message + ").\n" + 
                              "Check that the path is correct and the Guest account is enabled." );
                        }

                        return;

                    }
                    // The process is now being started as suspended. We should actually
                    // create our own IDirect3DDevice and get the pointer to the EndScene function
                    // and then save it, resume wow, do all our stuff and use that pointer when we 
                    // inject the LUA DLL

                    //Inject!!!!
                    Injector.InjectLua(process.Id);

                    // If we're not using autologin we make sure that the LUA hook is off the way until we are logged in
                    // Injector.Lua_RegisterInputHandler();

                    // resume
                    ResumeMainWowThread();

                    if (process != null)
                    {
                        afterProcessStart();
                        Log("char", "WoW started.");
                    }
                    else
                    {
                        Log("char", "Failed start WoW.");
                    }
                }
                else
                {
                    throw new Exception("Wow is not installed or the registry key is missing.");
                }
            }
            catch (Win32Exception w32e)
            {
                if (WoWProcessFailed != null)
                {
                    WoWProcessFailed(w32e.Message);
                }
            }
            catch (Exception ex)
            {
                if (WoWProcessAccessFailed != null)
                {
                    WoWProcessAccessFailed(ex.Message);
                }
            }
        }



        public static void ResetWayPoint()
        {
            if (PlayerWayPoint != null)
            {
                if (Player == null)
                {
                    throw new Exception("Cannot reset waypoints. No Player object found.");
                }
                Player.LastLocation = new Vector3D(0, 0, 0);
            }
        }

        /// <summary>
        /// Update all player informations: hp,mana,xp,position,etc...
        /// </summary>
        public static void UpdatePlayer()
        {
            if (_gstatus != GameStatuses.INITIALIZED)
            {
                return;
            }
            
            if (PlayerUpdate != null)
            {
                PlayerUpdate();
            }

            //update last player location
            Player.LastLocation = Player.Location;

            if (PlayerWayPoint != null)
            {
                Vector3D current = Player.Location;
                WayPoint wpLast = (WayPointManager.Instance.NormalNodeCount > 0) ? WayPointManager.Instance.NormalPath.Last() : null;

                if (wpLast != null && MathFuncs.GetDistance(current, wpLast.Location, false) > 5 || wpLast == null)
                {
                    PlayerWayPoint(current);
                }
            }

        }

        /// <summary>
        /// Check if you are logged into the WoW game
        /// </summary>
        public static void CheckInGame()
        {
            try
            {
                WowProcess.ReadUInt(WowProcess.ReadUInt(WowProcess.ReadUInt(Globals.GameOffset) +
                                                        Globals.PlayerBaseOffset1) + Globals.PlayerBaseOffset2);
                // Read successful. Check if we need initialize
                if (_gstatus != GameStatuses.INITIALIZED)
                {
                    _gstatus = GameStatuses.IN_WORLD;
                    Initialize();
                }
            }
            catch(Exception ex)
            {
                if (_gstatus == GameStatuses.IN_WORLD)
                {
                    Debug("char", "CheckInGame() - caugth exception: " + ex.Message);

                    _gstatus = GameStatuses.DISCONNECTED;
                }
                else
                    _gstatus = GameStatuses.INIT;
                
            }
        }

        /// <summary>
        /// Try to find the Thread Local Storage, aka WoW must be running
        /// </summary>
        public static bool FindTLS()
        {
            try
            {
                // search for the code pattern that we want (in this case, WoW TLS)
                TLS = SPattern.FindPattern(process.Handle, process.MainModule,
                                           "EB 02 33 C0 8B D 00 00 00 00 64 8B 15 00 00 00 00 8B 34 8A 8B D 00 00 00 00 89 81 00 00 00 00",
                                           "xxxxxx????xxx????xxxxx????xx????");


                if (TLS == uint.MaxValue)
                {
                    Debug("char", "Looking for the TLS returned an invalid value");
                    return false;
                }

                if (UpdateStatus != null)
                {
                    UpdateStatus("TLS found");
                    Debug("char", "TLS found");
                }

                return true;

            } catch (Exception)
            {
                Debug("char", "Cannot find the TLS");
                return false;
            }
        }

        private static bool InitializeConnectionManager()
        {
            try
            {
                Globals.ClientConnectionPointer = wowProcess.ReadUInt(TLS + 0x16);
                Globals.ClientConnection = wowProcess.ReadUInt(Globals.ClientConnectionPointer);
                if (Globals.ClientConnection == 0)
                {
                    Debug("char", "ClientConnection not yet available");
                    return false;
                }
                Globals.ClientConnectionOffset = wowProcess.ReadUInt(TLS + 0x1C);
                if (Globals.ClientConnectionOffset == 0)
                {
                    Debug("char", "ClientConnectionOffset not yet available");
                    return false;
                }
                Globals.CurMgr = wowProcess.ReadUInt(Globals.ClientConnection + Globals.ClientConnectionOffset);
                if (Globals.CurMgr == 0)
                {
                    Debug("char", "ConnectionManager not yet available");
                    return false;
                }
                //ObjectManager = new ObjectManager();
                //Player = new WowPlayer(ObjectManager.GetLocalPlayerObject());
                Debug("char", "Found ConnectionManager");
                return true;
            }
            catch(Exception)
            {
                Debug("char", "ConnectionManager not found");
                return false;
            }
        }

        private static void InitializePlayer()
        {
            ObjectManager = new ObjectManager();
            Player = new WowPlayer(ObjectManager.GetLocalPlayerObject());
            if (WoWInGame != null)
                WoWInGame();
        }

        /// <summary>
        /// Search for the TLS and Initialize the bot once the user is logged in
        /// </summary>
        public static void Initialize()
        {
            try
            {
                if (UpdateStatus != null)
                {
                    UpdateStatus("Initializing..");
                }

                while (!InitializeConnectionManager())
                {
                    if (UpdateStatus != null)
                    {
                        UpdateStatus("Looking for the ConnectionManager..");
                    }
                    Thread.Sleep(250);
                } 
                InitializePlayer();
                // This might have already been done, but since we could have autologin disabled
                // we do this again (there are no issues if you call this more than once anyway)
                Injector.Lua_RegisterInputHandler();
                InitializeCaronte();
                //ScriptHost.Start();
                //StateManager.Instance.Stop();

                _gstatus = GameStatuses.INITIALIZED;
            }
            catch (Exception ex)
            {
                throw new Exception("Initialize failed! - " + ex.ToString());
            }
        }

        public static void InitializeCaronte()
        {
            //string continent = "Azeroth";  // temporary fix to get things running while debugging LUA
            string continent = Player.GetCurrentMapContinent();
            
            Caronte.Init(continent);
            Log("char", string.Format(
                "Caronte initialized with continent '{0}'", continent));

            // Caronte.Init("Azeroth"); 

            // We generate a fake path once to initialize the chunk loader stuff
            //Pather.Graph.Path path = ProcessManager.Caronte.CalculatePath(new Pather.Graph.Location(Player.Location.X, Player.Location.Y, Player.Location.Z), 
            //    new Pather.Graph.Location(Player.Location.X+5, Player.Location.Y+5, Player.Location.Z));
        }

        public static void SuspendMainWowThread()
        {
            ProcessThread wowMainThread = SThread.GetMainThread(process.Id);
            IntPtr hThread = SThread.OpenThread(wowMainThread.Id);
            SThread.SuspendThread(hThread);
        }

        public static void ResumeMainWowThread()
        {
            ProcessThread wowMainThread = SThread.GetMainThread(process.Id);
            IntPtr hThread = SThread.OpenThread(wowMainThread.Id);
            SThread.ResumeThread(hThread);
        }

        /// <summary>
        /// Get AutoRun mode
        /// </summary>
        public static bool AutoRun
        {
            get { return arun; }
        }

        public static void SetAutoRun()
        {
            arun = true;
        }

        /// <summary>
        /// Return true when WoW.exe successfully started & dante injected
        /// </summary>
        public static bool ProcessRunning
        {
            get { return _pstatus == ProcessStatuses.RUNNING; }
        }
        
        /// <summary>
        /// Return Process Status
        /// </summary>
        public static ProcessStatuses ProcessState
        {
            get { return _pstatus; }
        }

        public static GameStatuses GameStatus
        {
            get { return _gstatus; }
        }

        /// <summary>
        /// Reset game status after disconnect or exception
        /// </summary>
        public static void ResetGameStatus()
        {
            _gstatus = GameStatuses.INIT;
        }

        public static Talents ReadTalents(string fname)
        {
            XmlSerializer s = new XmlSerializer(typeof(Talents));
            TextReader r = new StreamReader(fname);
            Talents talents = (Talents)s.Deserialize(r);
            talents.FullPath = fname;
            r.Close();

            return talents;
        }

        /// <summary>
        /// Load Application config file
        /// </summary>
        public static void LoadConfig()
        {
            var serializer = new Serializer<Config>();
            
            try
            {
                config = serializer.Load(ConfigFileName);

                // Check version of config file
                if (ProcessManager.ConfigVersion != config.Version)
                    throw new ConfigFileChangedException();


                // Decrypt auto-login password
                if (!string.IsNullOrEmpty(config.Account.LoginPassword))
                {
                    try
                    {
                        config.Account.DecryptPassword(
                                config.Account.LoginPassword);
                    }
                    catch (Exception)
                    {
                        // We couldn't decrypt the password for some reason. We reset it to blank.
                        config.Account.LoginPassword = "";
                    }
                }

                OnConfigurationChanged();
            }
            catch (FileNotFoundException)
            {
                // Show App configuration window for the first time run
                if (FirstTimeRun != null)
                    FirstTimeRun();
                else
                {
                    Console.WriteLine("Config file config.xml not found");
                    Environment.Exit(1);
                }

            }
            catch (ConfigFileChangedException)
            {
                if (ConfigFileChanged != null)
                    ConfigFileChanged();
                else
                {
                    ShowError("Config file config.xml has version " +
                        config.Version + " that different from application version " +
                        ProcessManager.ConfigVersion);
                    Environment.Exit(2);
                }
            }
            catch (WoWDataNotFoundException ex)
            {
                ShowError(ex.Message);
                Environment.Exit(3);
            }
            catch (Exception e)
            {
                ShowError("Unable load config.xml : " + e.Message);
                Environment.Exit(4);
            }
        }

        /// <summary>
        /// Save application config file
        /// </summary>
        public static void SaveConfig()
        {
            var serializer = new Serializer<Config>();

            // Remember current config version
            config.Version = ConfigVersion;
            serializer.Save(ConfigFileName, config);

            OnConfigurationChanged();
        }

        private static void OnConfigurationChanged()
        {
            // Check  mandatory directories
            string[] dirs = {config.LogParams.Dir,
                config.ProfilesDir + System.IO.Path.DirectorySeparatorChar + "Accounts",
                config.ProfilesDir + System.IO.Path.DirectorySeparatorChar + "Characters" };

            foreach (string dir in dirs)
                if (!Directory.Exists(dir))
                    Directory.CreateDirectory(dir);

            wversion = FindWoWVersionByName(config.WoWInfo.Version);
            if (wversion == null)
                throw new WoWDataNotFoundException(config.WoWInfo.Version);
        }

        public static WoWVersion FindWoWVersionByName(string version)
        {
            return wdata.FindVersion(version);
        }

        #endregion
    }

    public class ConfigFileChangedException : Exception
    {
        public ConfigFileChangedException() : 
            base("Configuration file doesn't match application settings") { }
    }

    public class WoWDataNotFoundException : Exception
    {
        public WoWDataNotFoundException(string version) :
            base("WoWData.xml doesn't contain data for WoW version '" + version + "'") { }
    }
}
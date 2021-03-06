﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CClash
{
    public enum CacheStoreType
    {
        FileCache,
        SQLite,
    }

    public sealed class Settings
    {
        public static bool DebugEnabled { get; set; }
        public static string DebugFile { get; set; }

        public static string MissLogFile {
            get
            {
                return Environment.GetEnvironmentVariable("CCLASH_MISSES");
            }
        }
        public static bool MissLogEnabled { 
            get 
            {
                return !string.IsNullOrEmpty(MissLogFile); 
            }
        }

        static Settings() { }

        static bool ConditionVarsAreTrue(string prefix)
        {
            var var = Environment.GetEnvironmentVariable(prefix + "_VAR");
            if (!string.IsNullOrEmpty(var))
            {
                var values = Environment.GetEnvironmentVariable(prefix + "_VALUES");
                if (!string.IsNullOrEmpty(values))
                {
                    var check = Environment.GetEnvironmentVariable(var);
                    var vlist = values.Split(',');
                    foreach (var v in vlist)
                    {
                        if (v == check) return true;
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// Attempt to cache and restore PDB files. If the target PDB already exists then we will count 
        /// that towards the common key and cache the file. If not we mark that it doesnt and cache the file.
        /// 
        /// If on a subsequent run, the pdb already exists exactly as it was when we cached it or is missing then
        /// we allow a hit.
        /// 
        /// This basically only works for pdb builds that were sequential.
        /// </summary>
        public static bool AttemptPDBCaching
        {
            get
            {
                return false;
                //TODO - fix other things before enabling this
                //return Environment.GetEnvironmentVariable("CCLASH_ATTEMPT_PDB_CACHE") == "yes";
            }
        }

        /// <summary>
        /// When an object compilation with pdb generation (Zi) is requested. Instead
        /// generate embedded debug info (Z7).
        /// </summary>
        public static bool ConvertObjPdbToZ7
        {
            get
            {
                return Environment.GetEnvironmentVariable("CCLASH_Z7_OBJ") == "yes";
            }
        }

        public static bool PipeSecurityEveryone {
            get {
                return Environment.GetEnvironmentVariable("CCLASH_LAX_PIPE") == "yes";
            }
        }

        static bool EnabledByConditions()
        {
            return ConditionVarsAreTrue("CCLASH_ENABLE_WHEN");
        }

        static bool DisabledByConditions()
        {
            return ConditionVarsAreTrue("CCLASH_DISABLE_WHEN");
        }

        public static bool Disabled
        {
            get
            {
                var dis = !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("CCLASH_DISABLED"));
                return (dis || DisabledByConditions()) && (!EnabledByConditions());
            }
        }

        static string cachedir = null;
        public static string CacheDirectory
        {
            get
            {
                if (cachedir == null)
                {
                    cachedir = Environment.GetEnvironmentVariable("CCLASH_DIR");
                    if (string.IsNullOrEmpty(cachedir))
                    {
                        var appdata = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                        cachedir = System.IO.Path.Combine(appdata, "cclash");
                    }
                    if (cachedir.Contains('"'))
                        cachedir = cachedir.Replace("\"", "");
                }
                return cachedir;
            }
            set
            {
                cachedir = value;
            }
        }

        public static bool PreprocessorMode
        {
            get
            {
                var dm = Environment.GetEnvironmentVariable("CCLASH_PPMODE");
                if (dm != null)
                {
                    return true;
                }
                return ConditionVarsAreTrue("CCLASH_PPMODE_WHEN");
            }
        }

        public static bool IsCygwin {
            get {
                if (Environment.GetEnvironmentVariable("NO_CCLASH_CYGWIN_FIX") == null)
                    return true;

                if (Environment.GetEnvironmentVariable("OSTYPE") == "cygwin")
                    return true;
                return false;
            }
        }

        public static bool TrackerMode
        {
            get
            {
                string tmode = Environment.GetEnvironmentVariable("CCLASH_TRACKER_MODE");
                return tmode == "yes";
            }
        }

        public static bool DirectMode
        {
            get
            {
                return !PreprocessorMode && !TrackerMode;
            }
        }

        public static bool ServiceMode
        {
            get
            {
                return Environment.GetEnvironmentVariable("CCLASH_SERVER") != null;
            }
        }

        private static int hashThreadCount;
        public static int HashThreadCount
        {
            get
            {
                if (hashThreadCount == 0) hashThreadCount = Environment.ProcessorCount;
                return hashThreadCount;
            }
            set
            {
                hashThreadCount = value;
            }
        }

        public static bool NoAutoRebuild
        {
            get
            {
                return Environment.GetEnvironmentVariable("CCLASH_AUTOREBUILD") == "no";
            }
        }

        static string GetString(string envvar, string defaultValue)
        {
            var env = Environment.GetEnvironmentVariable(envvar);
            if (env == null) return defaultValue;
            return env;
        }

        static int GetInteger(string envvar, int defaultValue)
        {
            int rv = defaultValue;
            var env = GetString(envvar, null);
            if (env != null)
            {
                Int32.TryParse(env, out rv);
                if (rv < 0) rv = 0;
            }
            return (int)rv;
        }

        public static int ServerQuitAfterIdleMinutes
        {
            get
            {
                return GetInteger("CCLASH_EXIT_IDLETIME", 0);
            }
        }

        public static int SlowObjectTimeout
        {
            get
            {
                return GetInteger("CCLASH_SLOWOBJ_TIMEOUT", 0);
            }
        }

        public static int MaxServerThreads
        {
            get
            {
                return GetInteger("CCLASH_MAX_SERVER_THREADS", 0);
            }
        }


        const CacheStoreType DefaultCacheType = CacheStoreType.FileCache;

        public static CacheStoreType CacheType
        {
            get
            {
                string st = GetString("CCLASH_CACHE_TYPE", "files");
                switch (st)
                {
                    case "sqlite":
                        return CacheStoreType.SQLite;

                    case "files":
                        return CacheStoreType.FileCache;
                }
                return DefaultCacheType;
            }
        }

        public static bool HonorCPPTimes
        {
            get
            {
                return GetString("CCLASH_HONOR_CPP_TIMES", "yes") == "yes";
            }
        }
        
    }
}

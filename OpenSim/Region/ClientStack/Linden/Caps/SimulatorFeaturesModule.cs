/*
 * Copyright (c) Contributors, http://opensimulator.org/
 * See CONTRIBUTORS.TXT for a full list of copyright holders.
 *
 * Redistribution and use in source and binary forms, with or without
 * modification, are permitted provided that the following conditions are met:
 *     * Redistributions of source code must retain the above copyright
 *       notice, this list of conditions and the following disclaimer.
 *     * Redistributions in binary form must reproduce the above copyright
 *       notice, this list of conditions and the following disclaimer in the
 *       documentation and/or other materials provided with the distribution.
 *     * Neither the name of the OpenSimulator Project nor the
 *       names of its contributors may be used to endorse or promote products
 *       derived from this software without specific prior written permission.
 *
 * THIS SOFTWARE IS PROVIDED BY THE DEVELOPERS ``AS IS'' AND ANY
 * EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
 * WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
 * DISCLAIMED. IN NO EVENT SHALL THE CONTRIBUTORS BE LIABLE FOR ANY
 * DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
 * (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
 * LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND
 * ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
 * (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
 * SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
 */

using Mono.Addins;
using Nini.Config;
using OpenMetaverse;
using OpenMetaverse.StructuredData;
using OpenSim.Framework;
using OpenSim.Framework.Servers.HttpServer;
using OpenSim.Region.Framework.Interfaces;
using OpenSim.Region.Framework.Scenes;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using Caps = OpenSim.Framework.Capabilities.Caps;

namespace OpenSim.Region.ClientStack.Linden
{
    /// <summary>
    /// SimulatorFeatures capability.
    /// </summary>
    /// <remarks>
    /// This is required for uploading Mesh.
    /// Since is accepts an open-ended response, we also send more information
    /// for viewers that care to interpret it.
    /// 
    /// NOTE: Part of this code was adapted from the Aurora project, specifically
    /// the normal part of the response in the capability handler.
    /// </remarks>
    [Extension(Path = "/OpenSim/RegionModules", NodeName = "RegionModule", Id = "SimulatorFeaturesModule")]
    public class SimulatorFeaturesModule : ISharedRegionModule, ISimulatorFeaturesModule
    {
//        private static readonly ILog m_log =
//            LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        public event SimulatorFeaturesRequestDelegate OnSimulatorFeaturesRequest;

        private Scene m_scene;

        /// <summary>
        /// Simulator features
        /// </summary>
        private OSDMap m_features = new OSDMap();
        private ReaderWriterLock m_featuresRwLock = new ReaderWriterLock();

        private string m_SearchURL = string.Empty;
        private string m_DestinationGuideURL = string.Empty;
        private bool m_ExportSupported = false;
        private string m_GridName = string.Empty;
        private string m_GridURL = string.Empty;

        #region ISharedRegionModule Members

        public void Initialise(IConfigSource source)
        {
            IConfig config = source.Configs["SimulatorFeatures"];

            if (config != null)
            {  
                // These are normaly set in their respective modules
                m_SearchURL = config.GetString("SearchServerURI", m_SearchURL);
                m_DestinationGuideURL = config.GetString ("DestinationGuideURI", m_DestinationGuideURL);
                m_ExportSupported = config.GetBoolean("ExportSupported", m_ExportSupported);
                m_GridURL = Util.GetConfigVarFromSections<string>(source, "GatekeeperURI",
                        new string[] { "Startup", "Hypergrid", "SimulatorFeatures" }, String.Empty);
                m_GridName = config.GetString("GridName", string.Empty);
                if (m_GridName == string.Empty)
                    m_GridName = Util.GetConfigVarFromSections<string>(source, "gridname",
                            new string[] { "GridInfo", "SimulatorFeatures" }, String.Empty);
            }

            AddDefaultFeatures();
        }

        public void AddRegion(Scene s)
        {
            m_scene = s;
            m_scene.EventManager.OnRegisterCaps += RegisterCaps;

            m_scene.RegisterModuleInterface<ISimulatorFeaturesModule>(this);
        }

        public void RemoveRegion(Scene s)
        {
            m_scene.EventManager.OnRegisterCaps -= RegisterCaps;
        }

        public void RegionLoaded(Scene s)
        {
            GetGridExtraFeatures(s);
        }

        public void PostInitialise()
        {
        }

        public void Close() { }

        public string Name { get { return "SimulatorFeaturesModule"; } }

        public Type ReplaceableInterface
        {
            get { return null; }
        }

        #endregion

        /// <summary>
        /// Add default features
        /// </summary>
        /// <remarks>
        /// TODO: These should be added from other modules rather than hardcoded.
        /// </remarks>
        private void AddDefaultFeatures()
        {
            m_featuresRwLock.AcquireWriterLock(-1);
            try
            {
                m_features["MeshRezEnabled"] = true;
                m_features["MeshUploadEnabled"] = true;
                m_features["MeshXferEnabled"] = true;
                m_features["PhysicsMaterialsEnabled"] = true;
    
                OSDMap typesMap = new OSDMap();
                typesMap["convex"] = true;
                typesMap["none"] = true;
                typesMap["prim"] = true;
                m_features["PhysicsShapeTypes"] = typesMap;
    
                // Extra information for viewers that want to use it
                // TODO: Take these out of here into their respective modules, like map-server-url
                OSDMap extrasMap;
                if(m_features.ContainsKey("OpenSimExtras"))
                {
                    extrasMap = (OSDMap)m_features["OpenSimExtras"];
                }
                else
                    extrasMap = new OSDMap();

                if (m_SearchURL != string.Empty)
                    extrasMap["search-server-url"] = m_SearchURL;
                if (!string.IsNullOrEmpty(m_DestinationGuideURL))
                    extrasMap["destination-guide-url"] = m_DestinationGuideURL;
                if (m_ExportSupported)
                    extrasMap["ExportSupported"] = true;
                if (m_GridURL != string.Empty)
                    extrasMap["GridURL"] = m_GridURL;
                if (m_GridName != string.Empty)
                    extrasMap["GridName"] = m_GridName;

                if (extrasMap.Count > 0)
                    m_features["OpenSimExtras"] = extrasMap;

            }
            finally
            {
                m_featuresRwLock.ReleaseWriterLock();
            }
        }

        public void RegisterCaps(UUID agentID, Caps caps)
        {
            IRequestHandler reqHandler
                = new RestHTTPHandler(
                    "GET", "/CAPS/" + UUID.Random(),
                    x => { return HandleSimulatorFeaturesRequest(x, agentID); }, "SimulatorFeatures", agentID.ToString());

            caps.RegisterHandler("SimulatorFeatures", reqHandler);
        }

        public void AddFeature(string name, OSD value)
        {
            m_featuresRwLock.AcquireWriterLock(-1);
            try
            {
                m_features[name] = value;
            }
            finally
            {
                m_featuresRwLock.ReleaseWriterLock();
            }
        }

        public bool RemoveFeature(string name)
        {
            m_featuresRwLock.AcquireWriterLock(-1);
            try
            {
                return m_features.Remove(name);
            }
            finally
            {
                m_featuresRwLock.ReleaseWriterLock();
            }
        }

        public bool TryGetFeature(string name, out OSD value)
        {
            m_featuresRwLock.AcquireReaderLock(-1);
            try
            {
                return m_features.TryGetValue(name, out value);
            }
            finally
            {
                m_featuresRwLock.ReleaseReaderLock();
            }
        }

        public OSDMap GetFeatures()
        {
            m_featuresRwLock.AcquireReaderLock(-1);
            try
            {
                return new OSDMap(m_features);
            }
            finally
            {
                m_featuresRwLock.ReleaseReaderLock();
            }
        }

        private OSDMap DeepCopy()
        {
            // This isn't the cheapest way of doing this but the rate
            // of occurrence is low (on sim entry only) and it's a sure
            // way to get a true deep copy.
            m_featuresRwLock.AcquireReaderLock(-1);
            try
            {
                OSD copy = OSDParser.DeserializeLLSDXml(OSDParser.SerializeLLSDXmlString(m_features));

                return (OSDMap)copy;
            }
            finally
            {
                m_featuresRwLock.ReleaseReaderLock();
            }
        }

        private Hashtable HandleSimulatorFeaturesRequest(Hashtable mDhttpMethod, UUID agentID)
        {
//            m_log.DebugFormat("[SIMULATOR FEATURES MODULE]: SimulatorFeatures request");

            OSDMap copy = DeepCopy();

            SimulatorFeaturesRequestDelegate handlerOnSimulatorFeaturesRequest = OnSimulatorFeaturesRequest;
            if (handlerOnSimulatorFeaturesRequest != null)
                handlerOnSimulatorFeaturesRequest(agentID, ref copy);

            //Send back data
            Hashtable responsedata = new Hashtable();
            responsedata["int_response_code"] = 200; 
            responsedata["content_type"] = "text/plain";
            responsedata["keepalive"] = false;

            responsedata["str_response_string"] = OSDParser.SerializeLLSDXmlString(copy);

            return responsedata;
        }

        /// <summary>
        /// Gets the grid extra features.
        /// </summary>
        /// <param name='featuresURI'>
        /// The URI Robust uses to handle the get_extra_features request
        /// </param>
        private void GetGridExtraFeatures(Scene scene)
        {
            Dictionary<string, object> extraFeatures = scene.GridService.GetExtraFeatures();

            m_featuresRwLock.AcquireWriterLock(-1);
            try
            {
                OSDMap extrasMap = new OSDMap();

                foreach(string key in extraFeatures.Keys)
                {
                    extrasMap[key] = (string)extraFeatures[key];

                    if (key == "ExportSupported")
                    {
                        bool.TryParse(extraFeatures[key].ToString(), out m_ExportSupported);
                    }
                }
                m_features["OpenSimExtras"] = extrasMap;
            }
            finally
            {
                m_featuresRwLock.ReleaseWriterLock();
            }
        }
    }
}

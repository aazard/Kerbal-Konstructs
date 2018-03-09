﻿using System;
using UnityEngine;
using KerbalKonstructs.Utilities;
using KerbalKonstructs.Modules;

namespace KerbalKonstructs.Core
{
    public class LaunchSite : KKFacility
    {

        [CFGSetting]
        public string LaunchPadTransform;
        [CFGSetting]
        public string LaunchSiteName = "";
        [CFGSetting]
        public string LaunchSiteAuthor = "";
        [CFGSetting]
        public SiteType LaunchSiteType = SiteType.Any;
        [CFGSetting]
        public string LaunchSiteLogo = null;
        [CFGSetting]
        public string LaunchSiteIcon = null;
        [CFGSetting]
        public string LaunchSiteDescription;
        [CFGSetting]
        public LaunchSiteCategory Category = LaunchSiteCategory.Other;
        [CFGSetting]
        public float LaunchSiteLength;
        [CFGSetting]
        public float LaunchSiteWidth;
        [CFGSetting]
        public bool LaunchSiteIsHidden = false;
        [CFGSetting]
        public bool ILSIsActive = false;

        [CareerSetting]
        public string favouriteSite = "No";
        [CareerSetting]
        public float MissionCount = 0f;
        [CareerSetting]
        public string MissionLog = "No Log";


        internal Texture logo = null;
        internal Texture icon = null;

        internal float refLon;
        internal float refLat;
        internal float refAlt;
        internal CelestialBody body;

        internal GameObject lsGameObject;
        internal PSystemSetup.SpaceCenterFacility facility = null;

        internal void ParseLSConfig(StaticInstance instance, ConfigNode cfgNode)
        {
            if (cfgNode != null)
            {
                base.ParseConfig(cfgNode);
            }

            lsGameObject = instance.gameObject;
            // this is might be slow
            staticInstance = instance;
            body = staticInstance.CelestialBody;

            if (!string.IsNullOrEmpty(LaunchSiteLogo))
            {
                logo = GameDatabase.Instance.GetTexture(LaunchSiteLogo, false);

                if (logo == null)
                    logo = GameDatabase.Instance.GetTexture(staticInstance.model.path + "/" + LaunchSiteLogo, false);
            }
            // use default logo
            if (logo == null)
                logo = GameDatabase.Instance.GetTexture("KerbalKonstructs/Assets/DefaultSiteLogo", false);

            if (!string.IsNullOrEmpty(LaunchSiteIcon))
            {

                icon = GameDatabase.Instance.GetTexture(LaunchSiteIcon, false);

                if (icon == null)
                    icon = GameDatabase.Instance.GetTexture(staticInstance.model.path + "/" + LaunchSiteIcon, false);
            }

            refLon = (float)Math.Round(KKMath.GetLongitudeInDeg(staticInstance.RadialPosition), 2);
            refLat = (float)Math.Round(KKMath.GetLatitudeInDeg(staticInstance.RadialPosition), 2);

            refAlt = staticInstance.RadiusOffset;

        }

    }
    public enum LaunchSiteCategory
    {
        Runway,
        RocketPad,
        Helipad,
        Waterlaunch,
        Other
    }

    public enum SiteType
    {
        VAB,
        SPH,
        Any
    }
}

﻿using System;
using System.Linq;
using System.IO;
using System.Collections.Generic;
using KerbalKonstructs.API;
using UnityEngine;
using KSP.UI.Screens;
using System.Reflection;
using KerbalKonstructs.Utilities;

namespace KerbalKonstructs.Core
{
    public class StaticObject
    {
        public Vector3 RadialPosition;

        public CelestialBody body;
        public float StaffCurrent;

        public GameObject gameObject;
        public PQSCity pqsCity;
        public StaticModel model;

        public UrlDir.UrlConfig configUrl;
        public String configPath;

        [KSPField]
        public Dictionary<string, object> settings = new Dictionary<string, object>();

        public Boolean editing;
        public Boolean preview;

        private Vector3 origScale;

        internal bool isActive = false;

        private List<Renderer> _rendererComponents;


        /// <summary>
        /// Updates the static instance with new settings
        /// </summary>
        public void update()
        {
            if (pqsCity != null)
            {
                pqsCity.repositionRadial = (Vector3)settings["RadialPosition"];
                pqsCity.repositionRadiusOffset = (float)settings["RadiusOffset"];
                pqsCity.reorientInitialUp = (Vector3)settings["Orientation"];
                pqsCity.reorientFinalAngle = (float)settings["RotationAngle"];
                pqsCity.transform.localScale = origScale * (float)settings["ModelScale"];
                pqsCity.Orientate();
            }
            // Notify modules about update
            foreach (StaticModule module in gameObject.GetComponents<StaticModule>())
                module.StaticObjectUpdate();

        }

        public object getSetting(string setting)
        {
            if (settings.ContainsKey(setting))
                return settings[setting];
            // Debug.Log("KK: Setting " + setting + " not found in instance of model " + model.config);
            object defaultValue = KKAPI.getInstanceSettings()[setting].getDefaultValue();

            if (defaultValue != null)
            {
                settings.Add(setting, defaultValue);
                return defaultValue;
            }
            else
            {
                Log.Normal("Setting " + setting + " not found in instance API. BUG BUG BUG.");
                return null;
            }
        }

        public void setSetting(string setting, object value)
        {
            if (settings.ContainsKey(setting))
            {
                settings[setting] = value;
            }
            else
            {
                settings.Add(setting, value);
            }
        }

        public void HighlightObject(Color highlightColor)
        {
            Renderer[] rendererList = gameObject.GetComponentsInChildren<Renderer>();
            _rendererComponents = new List<Renderer>(rendererList);

            foreach (Renderer renderer in _rendererComponents)
            {
                renderer.material.SetFloat("_RimFalloff", 1.8f);
                renderer.material.SetColor("_RimColor", highlightColor);
            }
        }

        public void ToggleAllColliders(bool enable)
        {
            Transform[] gameObjectList = gameObject.GetComponentsInChildren<Transform>();

            List<GameObject> colliderList = (from t in gameObjectList where t.gameObject.GetComponent<Collider>() != null select t.gameObject).ToList();

            foreach (GameObject gocollider in colliderList)
            {
                gocollider.GetComponent<Collider>().enabled = enable;
            }
        }

        public float GetDistanceToObject(Vector3 vPosition)
        {
            float fDistance = 0f;
            fDistance = Vector3.Distance(gameObject.transform.position, vPosition);
            return fDistance;
        }

        /// <summary>
        /// Removes the wreck model from an KSC Object.
        /// </summary>
        private void MangleSquadStatic()
        {
            Log.Normal("Mangle: " + model.name + " parent: " + gameObject.transform.name);
            gameObject.transform.parent = null;
            var transforms = gameObject.transform.GetComponentsInChildren<Transform>(true);
            foreach (var transform in transforms)
            {

                if (transform.name.Equals("wreck", StringComparison.InvariantCultureIgnoreCase))
                {
                    Log.Normal("deleting wreck 0");
                    transform.parent = null;
                    GameObject.Destroy(transform.gameObject);
                }
            }
        }



        public void spawnObject(Boolean editing, Boolean bPreview)
        {
            // mangle Squads statics
            if (model.isSquad)
            {
                MangleSquadStatic();
            }

            // Objects spawned at runtime should be active, ones spawned at loading not
            gameObject.SetActive(editing);

            Transform[] gameObjectList = gameObject.GetComponentsInChildren<Transform>();
            List<GameObject> rendererList = (from t in gameObjectList where t.gameObject.GetComponent<Renderer>() != null select t.gameObject).ToList();

            SetColliderLayer(15);

            if (bPreview) this.ToggleAllColliders(false);

            this.preview = bPreview;

            if (editing) KerbalKonstructs.instance.SelectObject(this, true, true, bPreview);

            float objvisibleRange = (float)getSetting("VisibilityRange");

            if (objvisibleRange < 1) objvisibleRange = 25000f;

            PQSCity.LODRange range = new PQSCity.LODRange
            {
                renderers = rendererList.ToArray(),
                objects = new GameObject[0],
                visibleRange = objvisibleRange
            };

            pqsCity = gameObject.AddComponent<PQSCity>();
            pqsCity.lod = new[] { range };
            pqsCity.frameDelta = 1; //Unknown
            pqsCity.repositionToSphere = true; //enable repositioning
            pqsCity.repositionToSphereSurface = false; //Snap to surface?
            pqsCity.repositionRadial = (Vector3)getSetting("RadialPosition"); //position
            pqsCity.repositionRadiusOffset = (float)getSetting("RadiusOffset"); //height
            pqsCity.reorientInitialUp = (Vector3)getSetting("Orientation"); //orientation
            pqsCity.reorientFinalAngle = (float)getSetting("RotationAngle"); //rotation x axis
            pqsCity.reorientToSphere = true; //adjust rotations to match the direction of gravity
            gameObject.transform.parent = ((CelestialBody)getSetting("CelestialBody")).pqsController.transform;
            pqsCity.sphere = ((CelestialBody)getSetting("CelestialBody")).pqsController;
            origScale = pqsCity.transform.localScale;             // save the original scale for later use
            pqsCity.transform.localScale *= (float)getSetting("ModelScale");
            pqsCity.order = 100;
            pqsCity.modEnabled = true;
            pqsCity.OnSetup();
            pqsCity.Orientate();

            body = (CelestialBody)getSetting("CelestialBody");

            // Add them to the bodys objectlist, so they show up as anomalies
            if (bool.Parse((string)getSetting("isScanable")))
            {
                Log.Normal("Added " + gameObject.name + " to scanable Objects");
                var pqsObjectList = body.pqsSurfaceObjects.ToList();
                pqsObjectList.Add(pqsCity as PQSSurfaceObject);
                body.pqsSurfaceObjects = pqsObjectList.ToArray();
            }

            foreach (StaticModule module in model.modules)
            {
                Type moduleType = AssemblyLoader.loadedAssemblies.SelectMany(asm => asm.assembly.GetTypes()).FirstOrDefault(t => t.Namespace == module.moduleNamespace && t.Name == module.moduleClassname);
                MonoBehaviour mod = gameObject.AddComponent(moduleType) as MonoBehaviour;

                if (mod != null)
                {
                    foreach (string fieldName in module.moduleFields.Keys)
                    {
                        FieldInfo field = mod.GetType().GetField(fieldName);
                        if (field != null)
                        {
                            field.SetValue(mod, Convert.ChangeType(module.moduleFields[fieldName], field.FieldType));
                        }
                        else
                        {
                            Debug.Log("KK: WARNING: Field " + fieldName + " does not exist in " + module.moduleClassname);
                        }
                    }
                }
                else
                {
                    Debug.Log("KK: WARNING: Module " + module.moduleClassname + " could not be loaded in " + gameObject.name);
                }
            }

            foreach (GameObject gorenderer in rendererList)
            {
                gorenderer.GetComponent<Renderer>().enabled = true;
            }
        }

        /// <summary>
        /// Sets the collider layer to a new layer 
        /// </summary>
        /// <param name="layerNumber"></param>
        internal void SetColliderLayer(int layerNumber)
        {
            SetLayerRecursively(gameObject, layerNumber);
        }


        /// <summary>
        /// iterates through the gameobject and its childs and sets the layer
        /// </summary>
        /// <param name="sGameObject"></param>
        /// <param name="newLayerNumber"></param>
		private void SetLayerRecursively(GameObject sGameObject, int newLayerNumber)
        {
            if (sGameObject.GetComponent<Collider>() == null) sGameObject.layer = newLayerNumber;
            else
                if (!sGameObject.GetComponent<Collider>().isTrigger) sGameObject.layer = newLayerNumber;


            foreach (Transform child in sGameObject.transform)
            {
                SetLayerRecursively(child.gameObject, newLayerNumber);
            }
        }

        /// <summary>
        /// Sets the StaticObject active or passive
        /// </summary>
        /// <param name="newState">new active state</param>
        internal void SetActive(bool newState)
        {
            if (this.isActive == newState)
            {
                return;
            }
            else
            {
                this.isActive = newState;

                foreach (StaticModule module in this.gameObject.GetComponents<StaticModule>())
                    module.StaticObjectUpdate();

                SetActiveRecursively(gameObject, newState);
            }
        }

        /// <summary>
        /// iterates through the gameobject and its childs and set them active
        /// </summary>
        /// <param name="rootObject"></param>
        /// <param name="active"></param>
        private void SetActiveRecursively(GameObject rootObject, bool active)
        {
            rootObject.SetActive(active);

            foreach (Transform childTransform in rootObject.transform)
            {
                SetActiveRecursively(childTransform.gameObject, active);
            }
        }

        public void deselectObject(Boolean enableColliders)
        {
            this.editing = false;
            if (enableColliders) this.ToggleAllColliders(true);

            Color highlightColor = new Color(0, 0, 0, 0);
            this.HighlightObject(highlightColor);
        }
    }
}
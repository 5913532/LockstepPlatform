﻿using System.Collections;
using System.Collections.Generic;
using Lockstep.Math;
using UnityEngine;

namespace Lockstep.Game {
 

    public partial class ResourceManager :IResourceService{
        private GameObject prefab;
        private Transform transParent;

        private GameConfig _config;
        public override void DoStart(){
            base.DoStart();
            transParent = new GameObject("GoParents").transform;
            prefab = GameObject.CreatePrimitive(PrimitiveType.Cube);
            prefab.SetActive(false);
            prefab.transform.SetParent(transParent, false);
            prefab.AddComponent<PositionListener>();
        }

        protected GameObject InstantiatePrefab(int configId){
            return UnityEngine.Object.Instantiate(prefab, transParent).gameObject;
            ;
        }
        
        public void ShowDiedEffect(LVector2 pos){
            GameObject.Instantiate(_config.DiedPrefab, transform.position + pos.ToVector3(), Quaternion.identity);
        }
        public void ShowBornEffect(LVector2 pos){
            GameObject.Instantiate(_config.BornPrefab, transform.position + pos.ToVector3(), Quaternion.identity);
        }
        
        
    }
}
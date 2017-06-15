using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NamespaceHeroesNetWorkView;

public class InitHeroesNetWorkView : MonoBehaviour {

    HeroesNetWorkView NetWorkView;

    void Awake()
    {
        Debug.Log("InitHeroesNetWorkView Awake");
        NetWorkView = HeroesNetWorkView.GetInstance;
    }
}

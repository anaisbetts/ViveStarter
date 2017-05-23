//
// Copyright (C) Valve Corporation. All rights reserved.
//

using UnityEngine;
using System;
using System.Threading;
using System.Runtime.InteropServices;

namespace Phonon
{
    //
    // Phonon Static Listener
    // Represents a component to update the latest node for baked static listener.
    //

    [AddComponentMenu("Phonon/Phonon Static Listener")]
    public class PhononStaticListener : MonoBehaviour
    {
        public BakedStaticListenerNode currentStaticListenerNode;
    }
}

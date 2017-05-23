//
// Copyright (C) Valve Corporation. All rights reserved.
//

using System;
using UnityEngine;

namespace Phonon
{

    //
    //	PhononMaterialValue
    //	Represents the values of a specific material.
    //

    [Serializable]
    public class PhononMaterialValue
    {

        //
        //	Constructor.
        //
        public PhononMaterialValue()
        {
        }

        //
        //	Constructor.
        //
        public PhononMaterialValue(float aLow, float aMid, float aHigh)
        {
            LowFreqAbsorption = aLow;
            MidFreqAbsorption = aMid;
            HighFreqAbsorption = aHigh;

            Scattering = 0.05f;
        }

        //
        // Constructor.
        //
        public PhononMaterialValue(float aLow, float aMid, float aHigh, float scattering)
        {
            LowFreqAbsorption = aLow;
            MidFreqAbsorption = aMid;
            HighFreqAbsorption = aHigh;

            Scattering = scattering;
        }

        //
        // Copy constructor.
        //
        public PhononMaterialValue(PhononMaterialValue other)
        {
            CopyFrom(other);
        }

        //
        // Copies data from another object.
        //
        public void CopyFrom(PhononMaterialValue other)
        {
            LowFreqAbsorption = other.LowFreqAbsorption;
            MidFreqAbsorption = other.MidFreqAbsorption;
            HighFreqAbsorption = other.HighFreqAbsorption;

            Scattering = other.Scattering;
        }

        //
        // Data members.
        //

        // Absorption coefficients.
        [Range(0.0f, 1.0f)]
        public float LowFreqAbsorption;
        [Range(0.0f, 1.0f)]
        public float MidFreqAbsorption;
        [Range(0.0f, 1.0f)]
        public float HighFreqAbsorption;

        // Scattering coefficients.
        [Range(0.0f, 1.0f)]
        public float Scattering;

    }
}
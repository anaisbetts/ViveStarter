//
// Copyright (C) Valve Corporation. All rights reserved.
//

namespace Phonon
{

    //
    // PhononMaterialPresetList
    // Represents a list of all available material presets.
    //

    public static class PhononMaterialPresetList
    {

        //
        // Checks whether or not the list has been updated.
        //
        static bool IsInitialized()
        {
            return (values != null);
        }

        //
        // Refreshes the list of presets.
        //
        public static void Initialize()
        {
            // Count the number of presets.
            int numPresets = 12;
            values = new PhononMaterialValue[numPresets];

            // Create all the built-in presets.
            values[0] = new PhononMaterialValue(0.10f, 0.20f, 0.30f);
            values[1] = new PhononMaterialValue(0.03f, 0.04f, 0.07f);
            values[2] = new PhononMaterialValue(0.05f, 0.07f, 0.08f);
            values[3] = new PhononMaterialValue(0.01f, 0.02f, 0.02f);
            values[4] = new PhononMaterialValue(0.60f, 0.70f, 0.80f);
            values[5] = new PhononMaterialValue(0.24f, 0.69f, 0.73f);
            values[6] = new PhononMaterialValue(0.06f, 0.03f, 0.02f);
            values[7] = new PhononMaterialValue(0.12f, 0.06f, 0.04f);
            values[8] = new PhononMaterialValue(0.11f, 0.07f, 0.06f);
            values[9] = new PhononMaterialValue(0.20f, 0.07f, 0.06f);
            values[10] = new PhononMaterialValue(0.13f, 0.20f, 0.24f);
            values[11] = new PhononMaterialValue();
        }

        //
        // Returns the values of a material by index.
        //
        public static PhononMaterialValue PresetValue(int index)
        {
            if (!IsInitialized())
                Initialize();

            return values[index];
        }

        //
        // Data members.
        //

        // Values of all presets.
        static PhononMaterialValue[] values;

    }
}
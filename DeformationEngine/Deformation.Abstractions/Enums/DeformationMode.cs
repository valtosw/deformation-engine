using System.ComponentModel;

namespace Deformation.Abstractions.Enums
{
    public enum DeformationMode
    {
        [Description("Basic Transformation")]
        Basic,

        [Description("Twist Deformation")]
        Twist,

        [Description("Bend Deformation")]
        Bend,

        [Description("Free-Form Deformation (FFD)")]
        FreeFormDeformation,

        [Description("As-Rigid-As-Possible (ARAP)")]
        AsRigidAsPossible,

        [Description("Linear Blend Skinning (LBS)")]
        LinearBlendSkinning
    }
}

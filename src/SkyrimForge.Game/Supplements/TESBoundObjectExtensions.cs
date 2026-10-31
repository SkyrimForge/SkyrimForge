using System;
using System.Linq;

namespace SkyrimForge.Game;

/// <summary>Provides extension members for <see cref="ITESBoundObject"/>.</summary>
public static class TESBoundObjectExtensions
{
    extension<TBoundObject>(TBoundObject boundObject) where TBoundObject : ITESBoundObject
    {
        internal float OriginDrop
        {
            get
            {
                TESBoundObject.BOUND_DATA bounds = boundObject.BoundData;
                return -(bounds.BoundMin.Z + bounds.BoundMax.Z) / 2f;
            }
        }

        /// <summary>Whether the object is a flora or tree record that specifies a produce item.</summary>
        public bool IsHarvestable
        {
            get
            {
                TESBoundObject baseObject = boundObject.AsTESBoundObject();
                return (baseObject.AsFlora() != null || baseObject.AsTreeObject() != null) && baseObject.AsTESProduceForm()?.ProduceItem != null;
            }
        }

        /// <summary>Whether the object is a tree, flora, or grass record, or a record whose model path contains a word for vegetation, such as tree or grass.</summary>
        public bool IsVegetation => boundObject.GetFormType() is FormType.Tree or FormType.Flora or FormType.Grass
            || (boundObject.AsTESBoundObject().AsTESModel()?.GetModel() is { } modelPath && vegetationWords.Any(word => modelPath.Contains(word, StringComparison.OrdinalIgnoreCase)));
    }

    private static readonly string[] vegetationWords = ["tree", "shrub", "bush", "plant", "flora", "fern", "grass", "stump", "deadwood"];
}

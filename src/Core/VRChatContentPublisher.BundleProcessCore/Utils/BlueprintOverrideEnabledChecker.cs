using AssetsTools.NET.Extra;
using AssetsTools.NET;

namespace VRChatContentPublisher.BundleProcessCore.Utils;

public static class BlueprintOverrideEnabledChecker
{
    public static bool IsBlueprintOverrideEnabled(
        AssetsManager assetsManager,
        AssetsFileInstance[] assetsFileInstances)
    {
        // Intentionally bypass blueprint override guard for manual upload workflow.
        //foreach (var assetsFileInstance in assetsFileInstances)
        //{
        //    foreach (var gameObjectInfo in assetsFileInstance.file.GetAssetsOfType(AssetClassID.GameObject))
        //    {
        //        var gameObjectBase = assetsManager.GetBaseField(assetsFileInstance, gameObjectInfo);
        //        if (gameObjectBase["m_Name"] is
        //            {
        //                IsDummy: false, Value.ValueType: AssetValueType.String,
        //                AsString: BundleProcessConst.AllowBlueprintOverrideKey
        //            })
        //        {
        //            return true;
        //        }
        //    }
        //}
        //
        //return false;

        _ = assetsManager;
        _ = assetsFileInstances;
        return true;
    }
}

using UnityEditor;
namespace BeforeJericho.Editor
{
    /// <summary>Import rules for Blender kit exports under Art/Models (sources in ArtSource/). Static meshes only; Blender material names
    /// ("Aged stucco", "Walnut", ...) resolve to the existing triplanar materials in Art/Materials, so kit pieces need no UVs.</summary>
    public class KitModelImport : AssetPostprocessor
    {
        void OnPreprocessModel()
        {
            if(!assetPath.StartsWith("Assets/_Game/Art/Models/")) return;
            var importer = (ModelImporter)assetImporter;
            importer.importCameras = false; importer.importLights = false; importer.importVisibility = false;
            importer.importAnimation = false; importer.animationType = ModelImporterAnimationType.None;
            importer.addCollider = false; // gameplay keeps the greybox colliders
            importer.materialImportMode = ModelImporterMaterialImportMode.ImportStandard;
            importer.materialLocation = ModelImporterMaterialLocation.External;
            importer.materialName = ModelImporterMaterialName.BasedOnMaterialName;
            importer.materialSearch = ModelImporterMaterialSearch.RecursiveUp; // finds Art/Materials/<name>.mat
        }
    }
}

using UnityEngine;
using UnityEditor;

namespace GirlsEvolution2D.Editor
{
    /// <summary>
    /// GridPaintingState 오류를 해결하기 위한 임시 스크립트
    /// </summary>
    public class GridPaintingStateFix : EditorWindow
    {
        [MenuItem("Tools/Fix GridPaintingState Error")]
        public static void FixGridPaintingStateError()
        {
            try
            {
                // GridPaintingState 초기화 강제
                var gridPaintingState = UnityEditor.Tilemaps.GridPaintingState.instance;
                if (gridPaintingState != null)
                {
                    Debug.Log("GridPaintingState 초기화 성공");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"GridPaintingState 초기화 실패: {e.Message}");
                
                // 대안: 에디터 재시작 권장
                if (EditorUtility.DisplayDialog("GridPaintingState 오류", 
                    "GridPaintingState 오류가 발생했습니다.\n\n해결 방법:\n1. Unity 에디터를 완전히 종료\n2. Library 폴더를 삭제\n3. Unity에서 프로젝트를 다시 열기\n\n지금 Unity를 재시작하시겠습니까?", 
                    "재시작", "취소"))
                {
                    EditorApplication.OpenProject(System.IO.Directory.GetCurrentDirectory());
                }
            }
        }
        
        [MenuItem("Tools/Clear Library Cache")]
        public static void ClearLibraryCache()
        {
            if (EditorUtility.DisplayDialog("캐시 정리", 
                "Library 폴더를 삭제하여 캐시를 정리하시겠습니까?\n\n주의: 이 작업 후 Unity를 재시작해야 합니다.", 
                "삭제", "취소"))
            {
                try
                {
                    string libraryPath = System.IO.Path.Combine(System.IO.Directory.GetCurrentDirectory(), "Library");
                    if (System.IO.Directory.Exists(libraryPath))
                    {
                        System.IO.Directory.Delete(libraryPath, true);
                        Debug.Log("Library 폴더가 삭제되었습니다. Unity를 재시작해주세요.");
                    }
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"Library 폴더 삭제 실패: {e.Message}");
                }
            }
        }
    }
} 
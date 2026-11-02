
using System.Collections;
using UnityEngine;

public class LoadingScreenTest : MonoBehaviour
{
    [SerializeField]
    private float testDuration = 5f;

    [ContextMenu("Test Loading Screen")]
    private void TestLoadingScreen()
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning(
                "[LoadingScreenTest] Execute o teste durante o Play Mode."
            );
            return;
        }

        if (LoadingScreenController.Instance == null)
        {
            Debug.LogError(
                "[LoadingScreenTest] LoadingScreenController não encontrado."
            );
            return;
        }

        StopAllCoroutines();
        StartCoroutine(RunTest());
    }

    private IEnumerator RunTest()
    {
        LoadingScreenController.Instance.Show(
            "Preparando partida..."
        );

        float elapsed = 0f;

        while (elapsed < testDuration)
        {
            elapsed += Time.deltaTime;

            float progress = Mathf.Clamp01(
                elapsed / testDuration
            );

            LoadingScreenController.Instance.SetProgress(progress);

            LoadingScreenController.Instance.SetStatus(
                $"Carregando... {(progress * 100f):0}%"
            );

            yield return null;
        }

        LoadingScreenController.Instance.SetProgress(1f);

        LoadingScreenController.Instance.SetStatus(
            "Carregamento concluído!"
        );

        yield return new WaitForSeconds(1.5f);

        LoadingScreenController.Instance.Hide();

        Debug.Log("[LoadingScreenTest] Teste concluído.");
    }
}

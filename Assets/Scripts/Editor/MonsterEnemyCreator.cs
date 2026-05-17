using UnityEditor;
using UnityEngine;
using UnityEngine.AI;

public static class MonsterEnemyCreator
{
    [MenuItem("GameObject/Murino/Create Monster Enemy", false, 10)]
    private static void CreateMonsterEnemy()
    {
        GameObject monster = new GameObject("Monster Enemy");
        Undo.RegisterCreatedObjectUndo(monster, "Create Monster Enemy");

        if (SceneView.lastActiveSceneView != null)
        {
            monster.transform.position = SceneView.lastActiveSceneView.pivot;
        }

        GameObject body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        Undo.RegisterCreatedObjectUndo(body, "Create Monster Body");
        body.name = "Body";
        body.transform.SetParent(monster.transform);
        body.transform.localPosition = Vector3.up;
        body.transform.localRotation = Quaternion.identity;
        body.transform.localScale = new Vector3(0.75f, 1f, 0.75f);

        GameObject eyePoint = new GameObject("EyePoint");
        Undo.RegisterCreatedObjectUndo(eyePoint, "Create Monster Eye Point");
        eyePoint.transform.SetParent(monster.transform);
        eyePoint.transform.localPosition = new Vector3(0f, 1.65f, 0.25f);
        eyePoint.transform.localRotation = Quaternion.identity;

        NavMeshAgent agent = monster.AddComponent<NavMeshAgent>();
        agent.height = 2f;
        agent.radius = 0.38f;
        agent.speed = 1.8f;
        agent.angularSpeed = 420f;
        agent.acceleration = 12f;
        agent.stoppingDistance = 0.15f;

        MonsterEnemyAI enemyAI = monster.AddComponent<MonsterEnemyAI>();
        SerializedObject serializedAI = new SerializedObject(enemyAI);
        serializedAI.FindProperty("eyePoint").objectReferenceValue = eyePoint.transform;
        serializedAI.ApplyModifiedPropertiesWithoutUndo();

        Selection.activeGameObject = monster;
    }
}

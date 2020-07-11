namespace com.faith.AI
{
    using System.Collections;
    using System.Collections.Generic;

    using UnityEngine;

    using com.faith.Gameplay;

    [RequireComponent(typeof(OnDemandPrefab))]
    public class NPCRoamingManager : MonoBehaviour
    {
        #region Custom Variables

        [System.Serializable]
        public struct NPCInfo
        {
            public int checkPointIndex;
            public float checkPointInitialDistance;
            public float npcMovementSpeed;
            public float remainingTimeForDelay;
            public Quaternion checkPointInitialRotation;
            public Transform npcTransformReference;
        }

        [System.Serializable]
        public struct NPCCheckPoint
        {
            public Transform checkPointPosition;
            [Space(5.0f)]
            public bool isRotateBySlerp;
            [Range(0f,5f)]
            public float delayOnCheckPoint;
            [Range(0f,1f)]
            public float speedMultiplier;
        }

        #endregion

        #region Public Variables

        public bool runOnStart = false;

        [Space(5.0f)]
        [Range(0, 1f)]
        public float spawnFrequency;

        [Space(5.0f)]
        [Range(0.0f, 0.5f)]
        public float npcSpeedVariation;
        [Range(0.25f, 20.0f)]
        public float npcMovementSpeed;

        [Space(5.0f)]
        public NPCCheckPoint[] npcCheckpoints;

        #endregion

        #region Private Variables

        private OnDemandPrefab m_NPCListReference;

        private List<NPCInfo> m_ActiveNPC;

        private bool m_IsForceStop;

        private bool m_IsNPCControllerForSpawnerRunning;
        private bool m_IsNPCControllerForMovementRunning;

        #endregion

        #region Mono Behaviour

        private void Awake()
        {
            m_NPCListReference = gameObject.GetComponent<OnDemandPrefab>();

            int t_NumberOfNPCCheckpoint = npcCheckpoints.Length;
            for (int counter = 0; counter < t_NumberOfNPCCheckpoint; counter++) {

                if (npcCheckpoints[counter].speedMultiplier == 0)
                    npcCheckpoints[counter].speedMultiplier = 1;
            }
        }

        private void Start()
        {
            //Debug Purpose
            if (runOnStart)
                PreProcess();
        }

        #endregion

        #region Configuretion

        private IEnumerator ControllerForNPCSpawner()
        {

            float t_CycleLength = 0.0167f;
            WaitForSeconds t_CycleDelay = new WaitForSeconds(t_CycleLength);

            float t_TimeDifferenceOnSpawn = (1f - spawnFrequency) * 60f;
            float t_RemainingTimeToSpawn = Random.Range(0, 5);

            while (m_IsNPCControllerForSpawnerRunning)
            {

                if (t_RemainingTimeToSpawn <= 0)
                {

                    t_RemainingTimeToSpawn = Random.Range(t_TimeDifferenceOnSpawn / 2.0f, t_TimeDifferenceOnSpawn);

                    GameObject t_NewNPC = Instantiate(
                            m_NPCListReference.GetObject(null),
                            transform.position,
                            Quaternion.identity);

                    Transform t_TransformReference = t_NewNPC.transform;
                    t_TransformReference.SetParent(transform);
                    t_TransformReference.position = npcCheckpoints[0].checkPointPosition.position;
                    t_TransformReference.rotation = npcCheckpoints[0].checkPointPosition.rotation;

                    NPCInfo t_NewNPCInfo = new NPCInfo()
                    {
                        checkPointIndex = 0,
                        remainingTimeForDelay = Random.Range((npcCheckpoints[0].delayOnCheckPoint - (npcCheckpoints[0].delayOnCheckPoint * npcSpeedVariation)), npcCheckpoints[0].delayOnCheckPoint),
                        npcTransformReference = t_TransformReference,
                        npcMovementSpeed = Random.Range((npcMovementSpeed - (npcMovementSpeed * npcSpeedVariation)), npcMovementSpeed)
                    };

                    m_ActiveNPC.Add(t_NewNPCInfo);
                }
                else
                {

                    t_RemainingTimeToSpawn -= t_CycleLength;
                }

                yield return t_CycleDelay;
            }

            StopCoroutine(ControllerForNPCSpawner());
        }

        private IEnumerator ControllerForNPCMovement()
        {
            float t_CycleLength = 0.0167f;
            WaitForSeconds t_CycleDelay = new WaitForSeconds(t_CycleLength);

            int t_ResetCheckPointIndex = npcCheckpoints.Length - 1;
            int t_NumberOfActiveNPC = 1;
            float t_DeltaTime;
            Vector3 t_ModifiedPosition;
            Quaternion t_ModifiedRotation;
            NPCInfo t_ModifiedNPCInfo;

            int t_NumberOfNPCToDestroy;
            List<int> t_IndexOfNPCToDestroy = new List<int>();

            while (m_IsNPCControllerForSpawnerRunning || (!m_IsForceStop && t_NumberOfActiveNPC > 0))
            {


                t_DeltaTime = Time.deltaTime;

                t_NumberOfActiveNPC = m_ActiveNPC.Count;
                for (int npcIndex = 0; npcIndex < t_NumberOfActiveNPC; npcIndex++)
                {

                    t_ModifiedNPCInfo = m_ActiveNPC[npcIndex];

                    if (Vector3.Distance(
                        t_ModifiedNPCInfo.npcTransformReference.position,
                        npcCheckpoints[t_ModifiedNPCInfo.checkPointIndex].checkPointPosition.position) <= 0.1f)
                    {

                        if (t_ModifiedNPCInfo.checkPointIndex == t_ResetCheckPointIndex)
                        {
                            // if : Last Check Point
                            t_IndexOfNPCToDestroy.Add(npcIndex);
                        }
                        else
                        {

                            t_ModifiedNPCInfo.remainingTimeForDelay = npcCheckpoints[t_ModifiedNPCInfo.checkPointIndex].delayOnCheckPoint;

                            t_ModifiedNPCInfo.checkPointInitialRotation = npcCheckpoints[t_ModifiedNPCInfo.checkPointIndex].checkPointPosition.rotation;
                            t_ModifiedNPCInfo.npcTransformReference.rotation = t_ModifiedNPCInfo.checkPointInitialRotation; 

                            t_ModifiedNPCInfo.checkPointIndex += 1;

                            t_ModifiedNPCInfo.checkPointInitialDistance = Vector3.Distance(
                                    t_ModifiedNPCInfo.npcTransformReference.position,
                                    npcCheckpoints[t_ModifiedNPCInfo.checkPointIndex].checkPointPosition.position
                                );

                            m_ActiveNPC[npcIndex] = t_ModifiedNPCInfo;
                        }
                    }
                    else
                    {

                        if (t_ModifiedNPCInfo.remainingTimeForDelay <= 0f)
                        {

                            if (npcCheckpoints[t_ModifiedNPCInfo.checkPointIndex].isRotateBySlerp)
                            {
                                t_ModifiedRotation = Quaternion.Slerp(
                                        t_ModifiedNPCInfo.checkPointInitialRotation,
                                        npcCheckpoints[t_ModifiedNPCInfo.checkPointIndex].checkPointPosition.rotation,
                                        1f - (Vector3.Distance(t_ModifiedNPCInfo.npcTransformReference.position, npcCheckpoints[t_ModifiedNPCInfo.checkPointIndex].checkPointPosition.position) / t_ModifiedNPCInfo.checkPointInitialDistance)
                                    );
                                t_ModifiedNPCInfo.npcTransformReference.rotation = t_ModifiedRotation;
                            }

                            t_ModifiedPosition = Vector3.MoveTowards(
                                    t_ModifiedNPCInfo.npcTransformReference.position,
                                    npcCheckpoints[t_ModifiedNPCInfo.checkPointIndex].checkPointPosition.position,
                                    t_ModifiedNPCInfo.npcMovementSpeed * npcCheckpoints[t_ModifiedNPCInfo.checkPointIndex].speedMultiplier * t_DeltaTime
                                );
                            t_ModifiedNPCInfo.npcTransformReference.position = t_ModifiedPosition;
                        }
                        else {

                            t_ModifiedNPCInfo.remainingTimeForDelay -= t_CycleLength;
                            m_ActiveNPC[npcIndex] = t_ModifiedNPCInfo;
                        }
                    }
                }

                //Clear List
                t_NumberOfNPCToDestroy = t_IndexOfNPCToDestroy.Count;
                for (int npcIndex = 0; npcIndex < t_NumberOfNPCToDestroy; npcIndex++)
                {

                    Destroy(m_ActiveNPC[t_IndexOfNPCToDestroy[npcIndex]].npcTransformReference.gameObject);
                    m_ActiveNPC.RemoveAt(t_IndexOfNPCToDestroy[npcIndex]);
                }

                t_IndexOfNPCToDestroy.Clear();
                m_ActiveNPC.TrimExcess();

                yield return t_CycleDelay;
            }

            if (m_IsForceStop)
            {
                t_NumberOfActiveNPC = m_ActiveNPC.Count;
                for (int index = 0; index < t_NumberOfActiveNPC; index++)
                {
                    Destroy(m_ActiveNPC[index].npcTransformReference.gameObject);
                }
            }

            m_IsNPCControllerForMovementRunning = false;
            m_ActiveNPC = null;

            StopCoroutine(ControllerForNPCMovement());
        }

        #endregion

        #region Public Callback

        public void PreProcess()
        {

            m_IsForceStop = false;

            if (m_ActiveNPC == null)
            {

                m_ActiveNPC = new List<NPCInfo>();
            }

            if (!m_IsNPCControllerForSpawnerRunning)
            {

                m_IsNPCControllerForSpawnerRunning = true;
                StartCoroutine(ControllerForNPCSpawner());
            }

            if (!m_IsNPCControllerForMovementRunning)
            {

                m_IsNPCControllerForMovementRunning = true;
                StartCoroutine(ControllerForNPCMovement());
            }
        }

        public void PostProcess(bool t_IsForceStop = false)
        {

            m_IsForceStop = t_IsForceStop;
            m_IsNPCControllerForSpawnerRunning = false;
        }

        #endregion
    }

}


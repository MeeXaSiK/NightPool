#if UNITY_EDITOR
using System;
using UnityEditor;

namespace NTC.Pool.ExecutionOrder
{
    [InitializeOnLoad]
    internal sealed class NightPoolGlobalExecutionOrder
    {
        static NightPoolGlobalExecutionOrder()
        {
            Type nightPoolType = typeof(NightPoolGlobal);
            
            foreach (MonoScript runtimeMonoScript in MonoImporter.GetAllRuntimeMonoScripts())
            {
                if (runtimeMonoScript.GetClass() != nightPoolType)
                    continue;
                
                int currentExecutionOrder = MonoImporter.GetExecutionOrder(runtimeMonoScript);

                if (currentExecutionOrder != Constants.NightPoolExecutionOrder)
                {
                    MonoImporter.SetExecutionOrder(runtimeMonoScript, Constants.NightPoolExecutionOrder);
                }
                
                return;
            }
        }
    }
}
#endif
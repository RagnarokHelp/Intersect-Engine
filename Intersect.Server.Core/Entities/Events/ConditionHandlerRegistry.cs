using Intersect.GameObjects;
using System.Reflection;
using Intersect.Framework.Core.GameObjects.Conditions;

namespace Intersect.Server.Entities.Events;

public static partial class ConditionHandlerRegistry
{
    private delegate bool HandleCondition(Condition condition, Player player, Event eventInstance, QuestDescriptor questDescriptor);
    private delegate bool HandleConditionBool<TCondition>(TCondition condition, Player player, Event eventInstance, QuestDescriptor questDescriptor) where TCondition : Condition;
    private static Dictionary<Type, HandleCondition> MeetsConditionFunctions = new Dictionary<Type, HandleCondition>();
    private static MethodInfo CreateWeaklyTypedDelegateForMethodInfoInfo;
    private static bool Initialized = false;
    private static object mLock = new object();


    public static void Init()
    {
        if (CreateWeaklyTypedDelegateForMethodInfoInfo == null)
            CreateWeaklyTypedDelegateForMethodInfoInfo = typeof(ConditionHandlerRegistry).GetMethod(nameof(CreateWeaklyTypedDelegateForConditionMethodInfo), BindingFlags.Static | BindingFlags.NonPublic);

        if (MeetsConditionFunctions.Count == 0)
        {
            var methods = typeof(Conditions).GetMethods(BindingFlags.DeclaredOnly | BindingFlags.Public | BindingFlags.Static).Where(m => m.Name == "MeetsCondition");
            foreach (var method in methods)
            {
                var conditionType = method.GetParameters()[0].ParameterType;
                var typedDelegateFactory = CreateWeaklyTypedDelegateForMethodInfoInfo.MakeGenericMethod(conditionType);

                var weakDelegate = typedDelegateFactory.Invoke(null, new object[] { method, null }) as HandleCondition;
                MeetsConditionFunctions.Add(conditionType, weakDelegate);
            }
        }

        Initialized = true;
    }

    public static bool CheckCondition(Condition condition, Player player, Event eventInstance, QuestDescriptor questDescriptor)
    {
        if (!Initialized)
        {
            lock (mLock)
            {
                if (!Initialized)
                {
                    Init();
                }
            }
        }

        return MeetsConditionFunctions[condition.GetType()](condition, player, eventInstance, questDescriptor);
    }


    private static HandleCondition CreateWeaklyTypedDelegateForConditionMethodInfo<TCondition>(MethodInfo methodInfo, object target = null) where TCondition : Condition
    {
        if (methodInfo == null)
        {
            throw new ArgumentNullException(nameof(methodInfo));
        }

        var stronglyTyped =
                Delegate.CreateDelegate(typeof(HandleConditionBool<TCondition>), target, methodInfo) as
                    HandleConditionBool<TCondition>;

        return (Condition condition, Player player, Event eventInstance, QuestDescriptor questBase) => stronglyTyped(
            (TCondition)condition, player, eventInstance, questBase
        );

        throw new ArgumentException($"Unsupported condition handler return type '{methodInfo.ReturnType.FullName}'.");
    }


}

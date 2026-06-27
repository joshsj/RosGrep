namespace ProjectA;

public class ReferencesInAssembly
{
    public static void ReferencesClass()
    {
        var instance = Class.FactoryMethod();

        instance.InstanceMethod();

        instance.SelfReferences();

        Class.StaticMethod();
    }
}
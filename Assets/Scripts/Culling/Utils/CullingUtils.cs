namespace Culling.Utils
{
    public static class CullingUtils
    {
        internal static float HaltonSequence(int index, int radix)
        {
            var result = 0f;
            var fraction = 1f / radix;

            while (index > 0)
            {
                result += (index % radix) * fraction;

                index /= radix;
                fraction /= radix;
            }

            return result;
        }
    }
}
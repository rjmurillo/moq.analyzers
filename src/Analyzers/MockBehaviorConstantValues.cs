namespace Moq.Analyzers;

internal static class MockBehaviorConstantValues
{
    /// <summary>
    /// Returns true when an operand has a constant value equal to a known MockBehavior field.
    /// </summary>
    /// <param name="operandConstant">The operand constant to compare.</param>
    /// <param name="knownBehaviorField">The known MockBehavior field to compare against.</param>
    /// <returns><see langword="true"/> when the constants are value-equal; otherwise, <see langword="false"/>.</returns>
    internal static bool ConstantValueEquals(Optional<object?> operandConstant, IFieldSymbol? knownBehaviorField)
    {
        if (knownBehaviorField is null)
        {
            return false;
        }

        System.Diagnostics.Debug.Assert(knownBehaviorField.HasConstantValue, "Known MockBehavior fields must expose constant values.");

        if (!operandConstant.HasValue)
        {
            return false;
        }

        return object.Equals(operandConstant.Value, knownBehaviorField.ConstantValue);
    }
}

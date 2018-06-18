namespace ZilfAnalyzers
{
    public static class DiagnosticIds
    {
        public const string ExceptionShouldUseDiagnosticCode = "ZILF0001";
        public const string DuplicateMessageCode = "ZILF0002";
        public const string DuplicateMessageFormat = "ZILF0003";
        public const string PrefixedMessageFormat = "ZILF0004";
        public const string ComparingZilObjectsWithEquals = "ZILF0005";
        public const string PartiallyOverriddenZilObjectComparison = "ZILF0006";
        public const string OverridingExactlyEqualsWithoutGetHashCode = "ZILF0007";
    }
}
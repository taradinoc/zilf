using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Data;
using System.Text;

namespace ZilfSourceGenerators
{
    /// <summary>
    /// Represents a single "part" of a signature, i.e., a method parameter,
    /// method return value, element of an array parameter, or field in a structure
    /// which is used in a signature.
    /// </summary>
    /// <remarks>
    /// This corresponds to a single value in the ZILF source code, but may
    /// potentially match any number of ZIL values at the call site.
    /// </remarks>
    /// <param name="FormalType">The type symbol representing the declared
    /// type of the corresponding source code element, i.e., the parameter
    /// type, return type, or field type.</param>
    abstract record Param(string FormalType);

    sealed record NamedParam(string Name, Param Param) : Param(Param.FormalType);

    sealed record OptionalParam(string FormalType, object? DefaultValue) : Param(FormalType);

    sealed record RestParam(string FormalType, Param Element) : Param(FormalType);

    /// <inheritdoc cref="Param" />
    /// <summary>
    /// Represents a sequence of values that are combined into a single signature part.
    /// </summary>
    /// <param name="Elements">The elements that make up the sequence.</param>
    sealed record SequenceParam(string FormalType, NamedParam[] Elements) : Param(FormalType);

    abstract record StructuredParam(string FormalType, NamedParam[] Elements) : Param(FormalType);

    sealed record ListParam(string FormalType, NamedParam[] Elements) : StructuredParam(FormalType, Elements);

    sealed record FormParam(string FormalType, NamedParam[] Elements) : StructuredParam(FormalType, Elements);

    sealed record AdeclParam(string FormalType, NamedParam[] Elements) : StructuredParam(FormalType, Elements);

    sealed record AlternativeParam(string FormalType, Param[] Options) : Param(FormalType);

    sealed record AnyParam(string FormalType) : Param(FormalType);

    sealed record ConstrainedParam(Param Param, string Decl) : Param(Param.FormalType);
}

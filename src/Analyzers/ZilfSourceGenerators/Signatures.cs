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
    abstract record Param(ITypeSymbol FormalType);

    sealed record NamedParam(string Name, Param Param) : Param(Param.FormalType);

    sealed record OptionalParam(ITypeSymbol FormalType, object? DefaultValue) : Param(FormalType);

    sealed record RestParam(ITypeSymbol FormalType, Param Element) : Param(FormalType);

    /// <inheritdoc cref="Param" />
    /// <summary>
    /// Represents a sequence of values that are combined into a single signature part.
    /// </summary>
    /// <param name="Elements">The elements that make up the sequence.</param>
    sealed record SequenceParam(ITypeSymbol FormalType, NamedParam[] Elements) : Param(FormalType);

    abstract record StructuredParam(ITypeSymbol FormalType, NamedParam[] Elements) : Param(FormalType);

    sealed record ListParam(ITypeSymbol FormalType, NamedParam[] Elements) : StructuredParam(FormalType, Elements);

    sealed record FormParam(ITypeSymbol FormalType, NamedParam[] Elements) : StructuredParam(FormalType, Elements);

    sealed record AdeclParam(ITypeSymbol FormalType, NamedParam[] Elements) : StructuredParam(FormalType, Elements);

    sealed record AlternativeParam(ITypeSymbol FormalType, Param[] Options) : Param(FormalType);

    sealed record AnyParam(ITypeSymbol FormalType) : Param(FormalType);

    sealed record ConstrainedParam(Param Param, string Decl) : Param(Param.FormalType);
}

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Xunit;
using Graffs;

namespace Graffs.Tests;

public class CoreInterfaceTests
{
    [Fact]
    public void GraphEdge_Constructor_WithValidNodes_CreatesEdge()
    {
        // Arrange
        var source = "A";
        var target = "B";
        
        // Act
        var edge = new GraphEdge<string>(source, target);
        
        // Assert
        Assert.Equal(source, edge.Source);
        Assert.Equal(target, edge.Target);
        Assert.Null(edge.Metadata);
    }
    
    [Fact]
    public void GraphEdge_Constructor_WithMetadata_CreatesEdgeWithMetadata()
    {
        // Arrange
        var source = "A";
        var target = "B";
        var metadata = new Dictionary<string, object> { ["weight"] = 5, ["type"] = "dependency" }
            .ToImmutableDictionary();
        
        // Act
        var edge = new GraphEdge<string>(source, target, metadata);
        
        // Assert
        Assert.Equal(source, edge.Source);
        Assert.Equal(target, edge.Target);
        Assert.NotNull(edge.Metadata);
        Assert.Equal(5, edge.Metadata!["weight"]);
        Assert.Equal("dependency", edge.Metadata["type"]);
    }
    
    [Fact]
    public void GraphEdge_Constructor_WithNullSource_ThrowsArgumentNullException()
    {
        // Arrange
        string? source = null;
        var target = "B";
        
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new GraphEdge<string>(source!, target));
    }
    
    [Fact]
    public void GraphEdge_Constructor_WithNullTarget_ThrowsArgumentNullException()
    {
        // Arrange
        var source = "A";
        string? target = null;
        
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new GraphEdge<string>(source, target!));
    }
    
    [Fact]
    public void GraphEdge_Equality_WithSameSourceAndTarget_ReturnsTrue()
    {
        // Arrange
        var edge1 = new GraphEdge<string>("A", "B");
        var edge2 = new GraphEdge<string>("A", "B");
        
        // Act & Assert
        Assert.True(edge1.Equals(edge2));
        Assert.True(edge1 == edge2);
        Assert.False(edge1 != edge2);
    }
    
    [Fact]
    public void GraphEdge_Equality_WithDifferentSourceOrTarget_ReturnsFalse()
    {
        // Arrange
        var edge1 = new GraphEdge<string>("A", "B");
        var edge2 = new GraphEdge<string>("A", "C");
        var edge3 = new GraphEdge<string>("B", "B");
        
        // Act & Assert
        Assert.False(edge1.Equals(edge2));
        Assert.False(edge1.Equals(edge3));
        Assert.False(edge1 == edge2);
        Assert.False(edge1 == edge3);
    }
    
    [Fact]
    public void GraphEdge_Equality_IgnoresMetadata()
    {
        // Arrange
        var metadata1 = new Dictionary<string, object> { ["weight"] = 5 }.ToImmutableDictionary();
        var metadata2 = new Dictionary<string, object> { ["weight"] = 10 }.ToImmutableDictionary();
        
        var edge1 = new GraphEdge<string>("A", "B", metadata1);
        var edge2 = new GraphEdge<string>("A", "B", metadata2);
        var edge3 = new GraphEdge<string>("A", "B");
        
        // Act & Assert
        Assert.True(edge1.Equals(edge2));
        Assert.True(edge1.Equals(edge3));
        Assert.True(edge2.Equals(edge3));
    }
    
    [Fact]
    public void GraphEdge_GetHashCode_WithSameSourceAndTarget_ReturnsSameHash()
    {
        // Arrange
        var edge1 = new GraphEdge<string>("A", "B");
        var edge2 = new GraphEdge<string>("A", "B");
        
        // Act
        var hash1 = edge1.GetHashCode();
        var hash2 = edge2.GetHashCode();
        
        // Assert
        Assert.Equal(hash1, hash2);
    }
    
    [Fact]
    public void GraphEdge_GetHashCode_IgnoresMetadata()
    {
        // Arrange
        var metadata = new Dictionary<string, object> { ["weight"] = 5 }.ToImmutableDictionary();
        var edge1 = new GraphEdge<string>("A", "B");
        var edge2 = new GraphEdge<string>("A", "B", metadata);
        
        // Act
        var hash1 = edge1.GetHashCode();
        var hash2 = edge2.GetHashCode();
        
        // Assert
        Assert.Equal(hash1, hash2);
    }
    
    [Fact]
    public void GraphEdge_ToString_WithoutMetadata_ReturnsFormattedString()
    {
        // Arrange
        var edge = new GraphEdge<string>("A", "B");
        
        // Act
        var result = edge.ToString();
        
        // Assert
        Assert.Equal("A → B", result);
    }
    
    [Fact]
    public void GraphEdge_ToString_WithMetadata_IncludesMetadataCount()
    {
        // Arrange
        var metadata = new Dictionary<string, object> 
        { 
            ["weight"] = 5, 
            ["type"] = "dependency" 
        }.ToImmutableDictionary();
        var edge = new GraphEdge<string>("A", "B", metadata);
        
        // Act
        var result = edge.ToString();
        
        // Assert
        Assert.Equal("A → B [2 metadata items]", result);
    }
    
    [Fact]
    public void GraphEdge_WithMetadata_StaticMethod_CreatesEdgeCorrectly()
    {
        // Arrange
        var source = "A";
        var target = "B";
        var metadata = new Dictionary<string, object> { ["weight"] = 5 };
        
        // Act
        var edge = GraphEdge<string>.WithMetadata(source, target, metadata);
        
        // Assert
        Assert.Equal(source, edge.Source);
        Assert.Equal(target, edge.Target);
        Assert.NotNull(edge.Metadata);
        Assert.Equal(5, edge.Metadata!["weight"]);
    }
    
    [Fact]
    public void GraphEdge_Immutability_CannotModifyAfterCreation()
    {
        // Arrange
        var metadata = new Dictionary<string, object> { ["weight"] = 5 };
        var edge = GraphEdge<string>.WithMetadata("A", "B", metadata);
        
        // Act - modify original metadata
        metadata["weight"] = 10;
        metadata["newKey"] = "newValue";
        
        // Assert - edge metadata unchanged
        Assert.Equal(5, edge.Metadata!["weight"]);
        Assert.False(edge.Metadata.ContainsKey("newKey"));
    }
    
    [Fact]
    public void GraphEdge_EqualityWithNull_ReturnsFalse()
    {
        // Arrange
        var edge = new GraphEdge<string>("A", "B");
        
        // Act & Assert
        Assert.False(edge.Equals(null));
        Assert.False(edge == null);
        Assert.False(null == edge);
        Assert.True(edge != null);
        Assert.True(null != edge);
    }
    
    [Fact]
    public void GraphEdge_EqualityWithSameReference_ReturnsTrue()
    {
        // Arrange
        var edge = new GraphEdge<string>("A", "B");
        
        // Act & Assert
        Assert.True(edge.Equals(edge));
        Assert.Same(edge, edge);
    }
}

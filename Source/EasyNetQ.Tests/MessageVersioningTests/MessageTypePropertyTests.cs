using System.Text;
using EasyNetQ.MessageVersioning;

namespace EasyNetQ.Tests.MessageVersioningTests;

public class MessageTypePropertyTests
{
    private const string AlternativeMessageTypesHeaderKey = "Alternative-Message-Types";

    // All types missing - GetType == exception
    [Fact]
    public void GetMessageType_returns_message_type_for_an_unversioned_message()
    {
        var typeNameSerialiser = new DefaultTypeNameSerializer();
        var property = MessageTypeProperty.CreateForMessageType(typeof(MyMessage), typeNameSerialiser);

        var messageType = property.GetMessageType();
        Assert.Equal(typeof(MyMessage), messageType);
    }

    [Fact]
    public void AppendTo_sets_message_type_and_no_alternatives_for_an_unversioned_message()
    {
        var typeNameSerialiser = new DefaultTypeNameSerializer();
        var property = MessageTypeProperty.CreateForMessageType(typeof(MyMessage), typeNameSerialiser);
        var properties = new MessageProperties();

        properties = property.AppendTo(properties);

        Assert.Equal(properties.Type, typeNameSerialiser.Serialize(typeof(MyMessage)));
        Assert.True(properties.Headers is null);
    }

    [Fact]
    public void GetMessageType_returns_message_type_for_a_versioned_message()
    {
        var typeNameSerialiser = new DefaultTypeNameSerializer();
        var property = MessageTypeProperty.CreateForMessageType(typeof(MyMessageV2), typeNameSerialiser);

        var messageType = property.GetMessageType();
        Assert.Equal(typeof(MyMessageV2), messageType);
    }

    [Fact]
    public void AppendTo_sets_message_type_and_alternatives_for_a_versioned_message()
    {
        var typeNameSerialiser = new DefaultTypeNameSerializer();
        var property = MessageTypeProperty.CreateForMessageType(typeof(MyMessageV2), typeNameSerialiser);
        var properties = new MessageProperties();

        properties = property.AppendTo(properties);

        Assert.Equal(properties.Type, typeNameSerialiser.Serialize(typeof(MyMessageV2)));
        Assert.Equal(Encoding.UTF8.GetString((byte[])properties.Headers[AlternativeMessageTypesHeaderKey]), typeNameSerialiser.Serialize(typeof(MyMessage)));
    }

    [Fact]
    public void MessageTypeProperty_is_created_correctly_from_message_properties_for_unversioned_message()
    {
        var typeNameSerialiser = new DefaultTypeNameSerializer();
        var properties = new MessageProperties { Type = typeNameSerialiser.Serialize(typeof(MyMessage)) };

        var property = MessageTypeProperty.ExtractFromProperties(properties, typeNameSerialiser);
        var messageType = property.GetMessageType();

        Assert.Equal(typeof(MyMessage), messageType);
    }

    [Fact]
    public void MessageTypeProperty_is_created_correctly_from_message_properties_for_versioned_message()
    {
        var typeNameSerializer = new DefaultTypeNameSerializer();
        var encodedAlternativeMessageTypes = Encoding.UTF8.GetBytes(typeNameSerializer.Serialize(typeof(MyMessage)));

        var properties = new MessageProperties { Type = typeNameSerializer.Serialize(typeof(MyMessageV2)) }
            .SetHeader(AlternativeMessageTypesHeaderKey, encodedAlternativeMessageTypes);
        var property = MessageTypeProperty.ExtractFromProperties(properties, typeNameSerializer);
        var messageType = property.GetMessageType();

        Assert.Equal(typeof(MyMessageV2), messageType);
    }

    [Fact]
    public void GetType_returns_first_available_alternative_if_message_type_unavailable()
    {
        var typeNameSerializer = new DefaultTypeNameSerializer();
        var v1 = typeNameSerializer.Serialize(typeof(MyMessage));
        var v2 = typeNameSerializer.Serialize(typeof(MyMessageV2));
        var vUnknown = v2.Replace("MyMessageV2", "MyUnknownMessage");
        var alternativeTypes = string.Concat(v2, ";", v1);
        var encodedAlternativeTypes = Encoding.UTF8.GetBytes(alternativeTypes);

        var properties = new MessageProperties { Type = vUnknown }
            .SetHeader(AlternativeMessageTypesHeaderKey, encodedAlternativeTypes);

        var property = MessageTypeProperty.ExtractFromProperties(properties, typeNameSerializer);
        var messageType = property.GetMessageType();

        Assert.Equal(typeof(MyMessageV2), messageType);
    }

    [Fact]
    public void GetType_returns_first_available_alternative_if_message_type_and_some_alternatives_unavailable()
    {
        var typeNameSerializer = new DefaultTypeNameSerializer();
        var v1 = typeNameSerializer.Serialize(typeof(MyMessage));
        var v2 = typeNameSerializer.Serialize(typeof(MyMessageV2));
        var vUnknown1 = v2.Replace("MyMessageV2", "MyUnknownMessage");
        var vUnknown2 = v2.Replace("MyMessageV2", "MyUnknownMessageV2");
        var alternativeTypes = string.Concat(vUnknown1, ";", v2, ";", v1);
        var encodedAlternativeTypes = Encoding.UTF8.GetBytes(alternativeTypes);

        var properties = new MessageProperties { Type = vUnknown2 }
            .SetHeader(AlternativeMessageTypesHeaderKey, encodedAlternativeTypes);

        var property = MessageTypeProperty.ExtractFromProperties(properties, typeNameSerializer);
        var messageType = property.GetMessageType();

        Assert.Equal(typeof(MyMessageV2), messageType);
    }

    [Fact]
    public void GetType_throws_exception_if_all_types_unavailable()
    {
        var typeNameSerialiser = new DefaultTypeNameSerializer();
        var v2 = typeNameSerialiser.Serialize(typeof(MyMessageV2));
        var vUnknown1 = v2.Replace("MyMessageV2", "MyUnknownMessage");
        var vUnknown2 = v2.Replace("MyMessageV2", "MyUnknownMessageV2");
        var vUnknown3 = v2.Replace("MyMessageV2", "MyUnknownMessageV3");
        var alternativeTypes = string.Concat(vUnknown2, ";", vUnknown1);
        var encodedAlternativeTypes = Encoding.UTF8.GetBytes(alternativeTypes);

        var properties = new MessageProperties { Type = vUnknown3 }
            .SetHeader(AlternativeMessageTypesHeaderKey, encodedAlternativeTypes);

        var property = MessageTypeProperty.ExtractFromProperties(properties, typeNameSerialiser);
        Assert.Throws<EasyNetQException>(() => property.GetMessageType());
    }
}

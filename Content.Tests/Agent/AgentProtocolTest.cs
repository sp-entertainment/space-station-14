using Content.Client.Agent;
using NUnit.Framework;

namespace Content.Tests.Agent;

[TestFixture]
public sealed class AgentProtocolTest
{
    [Test]
    public void AcceptsM1Actions()
    {
        Assert.Multiple(() =>
        {
            Assert.That(Parse("""{"v":0,"id":"m1","based_on":100,"type":"move","direction":[1,0],"duration_ms":100}"""), Is.EqualTo("move"));
            Assert.That(Parse("""{"v":0,"id":"i1","based_on":100,"type":"interact","target":"e44"}"""), Is.EqualTo("interact"));
            Assert.That(Parse("""{"v":0,"id":"s1","based_on":100,"type":"say","text":"Agent bridge online."}"""), Is.EqualTo("say"));
        });
    }

    [TestCase("""{"v":1,"id":"x","based_on":100,"type":"wait"}""", "protocol-invalid")]
    [TestCase("""{"v":0,"id":"x","based_on":100,"type":"dance"}""", "unknown-action")]
    [TestCase("""{"v":0,"id":"x","based_on":100,"type":"interact","target":"e0"}""", "protocol-invalid")]
    [TestCase("""{"v":0,"id":"x","based_on":100,"type":"say","text":""}""", "protocol-invalid")]
    [TestCase("""{"v":0,"id":"x","based_on":1,"type":"wait"}""", "stale")]
    public void RejectsInvalidActions(string json, string expectedError)
    {
        Assert.That(AgentProtocol.TryParseAction(json, 200, out _, out var error), Is.False);
        Assert.That(error, Is.EqualTo(expectedError));
    }

    [Test]
    public void RejectsOversizedMessages()
    {
        var json = new string('x', AgentProtocol.MaxMessageBytes + 1);

        Assert.That(AgentProtocol.TryParseAction(json, 1, out _, out var error), Is.False);
        Assert.That(error, Is.EqualTo("message-size"));
    }

    [Test]
    public void SerializesVersionedResults()
    {
        var json = AgentProtocol.Serialize(new AgentResult(0, "a1", 42, "rejected", "stale"));

        Assert.That(json, Is.EqualTo("""{"v":0,"id":"a1","tick":42,"status":"rejected","reason":"stale","type":"result"}"""));
    }

    private static string Parse(string json)
    {
        Assert.That(AgentProtocol.TryParseAction(json, 100, out var action, out var error), Is.True, error);
        return action!.Type;
    }
}

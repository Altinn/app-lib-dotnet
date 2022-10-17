using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Altinn.App.Core.Interface;
using Altinn.App.Core.Internal.Process;
using FluentAssertions;
using Moq;
using Xunit;

namespace Altinn.App.PlatformServices.Tests.Internal.Process;

public class ProcessReaderTests
{
    private readonly string _testDataPath = Path.Combine("Internal", "Process", "TestData");

    [Fact]
    public void TestBpmnRead()
    {
        ProcessReader pr = SetupProcessReader("simple-gateway.bpmn");
        BpmnReader br = SetupBpmnReader("simple-gateway.bpmn");
        pr.GetStartEventIds().Should().Equal("StartEvent").And.Equal(br.StartEvents());
        pr.GetProcessTaskIds().Should().Equal("Task1", "Task2").And.Equal(br.Tasks());
        pr.GetEndEventIds().Should().Equal("EndEvent").And.Equal(br.EndEvents());
        pr.GetSequenceFlowIds().Should().Equal("Flow1", "Flow2", "Flow3", "Flow4", "Flow5");
        pr.GetExclusiveGatewayIds().Should().Equal("Gateway1");
    }

    [Fact]
    public void GetNextElement_returns_gateway_if_follow_gateway_false()
    {
        var currentElement = "Task1";
        ProcessReader pr = SetupProcessReader("simple-gateway.bpmn");
        List<string> nextElements = pr.GetNextElementIds(currentElement, false, false);
        nextElements.Should().Equal("Gateway1");
    }

    [Fact]
    public void GetNextElement_returns_gateways_next_if_follow_gateway_true()
    {
        var bpmnfile = "simple-gateway.bpmn";
        var currentElement = "Task1";
        ProcessReader pr = SetupProcessReader(bpmnfile);
        List<string> nextElements = pr.GetNextElementIds(currentElement, true, false);
        nextElements.Should().Equal("Task2", "EndEvent");
        BpmnReader br = SetupBpmnReader(bpmnfile);
        nextElements.Should().Equal(br.NextElements(currentElement, true));
    }

    [Fact]
    public void GetNextElement_returns_gateways_default_next_if_follow_gateway_true_and_use_default()
    {
        var bpmnfile = "simple-gateway-default.bpmn";
        var currentElement = "Task1";
        ProcessReader pr = SetupProcessReader(bpmnfile);
        List<string> nextElements = pr.GetNextElementIds(currentElement, true, true);
        nextElements.Should().Equal("Task2");
        BpmnReader br = SetupBpmnReader(bpmnfile);
        nextElements.Should().Equal(br.NextElements(currentElement, false));
    }

    [Fact]
    public void GetNextElement_returns_gateways_next_if_follow_gateway_true_and_use_default_false()
    {
        var bpmnfile = "simple-gateway-default.bpmn";
        var currentElement = "Task1";
        ProcessReader pr = SetupProcessReader(bpmnfile);
        List<string> nextElements = pr.GetNextElementIds(currentElement, true, false);
        nextElements.Should().Equal("Task2", "EndEvent");
        BpmnReader br = SetupBpmnReader(bpmnfile);
        nextElements.Should().Equal(br.NextElements(currentElement, true));
    }
    
    [Fact]
    public void GetNextElement_returns_task1_in_simple_process()
    {
        var bpmnfile = "simple-linear.bpmn";
        var currentElement = "StartEvent";
        ProcessReader pr = SetupProcessReader(bpmnfile);
        List<string> nextElements = pr.GetNextElementIds(currentElement, true, true);
        nextElements.Should().Equal("Task1");
        BpmnReader br = SetupBpmnReader(bpmnfile);
        nextElements.Should().Equal(br.NextElements(currentElement, false));
    }
    
    [Fact]
    public void GetNextElement_returns_task2_in_simple_process()
    {
        var bpmnfile = "simple-linear.bpmn";
        var currentElement = "Task1";
        ProcessReader pr = SetupProcessReader(bpmnfile);
        List<string> nextElements = pr.GetNextElementIds(currentElement, true, true);
        nextElements.Should().Equal("Task2");
        BpmnReader br = SetupBpmnReader(bpmnfile);
        nextElements.Should().Equal(br.NextElements(currentElement, false));
    }
    
    [Fact]
    public void GetNextElement_returns_endevent_in_simple_process()
    {
        var bpmnfile = "simple-linear.bpmn";
        var currentElement = "Task2";
        ProcessReader pr = SetupProcessReader(bpmnfile);
        List<string> nextElements = pr.GetNextElementIds(currentElement, true, true);
        nextElements.Should().Equal("EndEvent");
        BpmnReader br = SetupBpmnReader(bpmnfile);
        nextElements.Should().Equal(br.NextElements(currentElement, false));
    }
    
    [Fact]
    public void GetNextElement_returns_emptylist_if_task_without_output()
    {
        var bpmnfile = "simple-no-end.bpmn";
        var currentElement = "Task2";
        ProcessReader pr = SetupProcessReader(bpmnfile);
        List<string> nextElements = pr.GetNextElementIds(currentElement, true, true);
        nextElements.Should().HaveCount(0);
        BpmnReader br = SetupBpmnReader(bpmnfile);
        nextElements.Should().Equal(br.NextElements(currentElement, false));
    }
    
    [Fact]
    public void GetNextElement_currentElement_null()
    {
        var bpmnfile = "simple-linear.bpmn";
        ProcessReader pr = SetupProcessReader(bpmnfile);
        Exception actualException = null;
        try
        {
            pr.GetNextElementIds(null!, true, true);
        }
        catch (Exception e)
        {
            actualException = e;
        }

        actualException.Should().BeOfType<ArgumentNullException>();
        BpmnReader br = SetupBpmnReader(bpmnfile);
        Exception expectedException = null;
        try
        {
            br.NextElements(null!, false);
        }
        catch (Exception e)
        {
            expectedException = e;
        }
        actualException.Should().BeOfType(expectedException?.GetType());
    }
    
    [Fact]
    public void GetNextElement_throws_exception_if_step_not_found()
    {
        var bpmnfile = "simple-linear.bpmn";
        var currentElement = "NoStep";
        ProcessReader pr = SetupProcessReader(bpmnfile);
        Exception actualException = null;
        try
        {
            pr.GetNextElementIds(currentElement, true, true);
        }
        catch (Exception e)
        {
            actualException = e;
        }

        actualException.Should().BeOfType<ProcessException>();
        BpmnReader br = SetupBpmnReader(bpmnfile);
        Exception expectedException = null;
        try
        {
            br.NextElements(currentElement, false);
        }
        catch (Exception e)
        {
            expectedException = e;
        }
        actualException.Should().BeOfType(expectedException?.GetType());
    }

    [Fact]
    public void GetElementInfo_returns_correct_info_for_ProcessTask()
    {
        var bpmnfile = "simple-gateway-default.bpmn";
        var currentElement = "Task1";
        ProcessReader pr = SetupProcessReader(bpmnfile);
        var actual = pr.GetElementInfo(currentElement);
        BpmnReader br = SetupBpmnReader(bpmnfile);
        var expected = br.GetElementInfo(currentElement);
        actual.Should().BeEquivalentTo(expected);
    }
    
    [Fact]
    public void GetElementInfo_returns_correct_info_for_StartEvent()
    {
        var bpmnfile = "simple-gateway-default.bpmn";
        var currentElement = "StartEvent";
        ProcessReader pr = SetupProcessReader(bpmnfile);
        var actual = pr.GetElementInfo(currentElement);
        BpmnReader br = SetupBpmnReader(bpmnfile);
        var expected = br.GetElementInfo(currentElement);
        actual.Should().BeEquivalentTo(expected);
    }
    
    [Fact]
    public void GetElementInfo_returns_correct_info_for_EndEvent()
    {
        var bpmnfile = "simple-gateway-default.bpmn";
        var currentElement = "EndEvent";
        ProcessReader pr = SetupProcessReader(bpmnfile);
        var actual = pr.GetElementInfo(currentElement);
        BpmnReader br = SetupBpmnReader(bpmnfile);
        var expected = br.GetElementInfo(currentElement);
        actual.Should().BeEquivalentTo(expected);
    }
    
    [Fact]
    public void GetElementInfo_returns_null_for_ExclusiveGateway()
    {
        var bpmnfile = "simple-gateway-default.bpmn";
        var currentElement = "Gateway1";
        ProcessReader pr = SetupProcessReader(bpmnfile);
        var actual = pr.GetElementInfo(currentElement);
        BpmnReader br = SetupBpmnReader(bpmnfile);
        var expected = br.GetElementInfo(currentElement);
        actual.Should().BeEquivalentTo(expected);
    }
    
    [Fact]
    public void GetElementInfo_throws_argument_null_expcetion_when_elementName_is_null()
    {
        var bpmnfile = "simple-linear.bpmn";
        ProcessReader pr = SetupProcessReader(bpmnfile);
        Exception actualException = null;
        try
        {
            pr.GetElementInfo(null!);
        }
        catch (Exception e)
        {
            actualException = e;
        }

        actualException.Should().BeOfType<ArgumentNullException>();
        BpmnReader br = SetupBpmnReader(bpmnfile);
        Exception expectedException = null;
        try
        {
            br.GetElementInfo(null!);
        }
        catch (Exception e)
        {
            expectedException = e;
        }
        actualException.Should().BeOfType(expectedException?.GetType());
    }

    [Fact]
    public void GetSequenceFlowsBetween_returns_all_sequenceflows_between_StartEvent_and_Task1()
    {
        var bpmnfile = "simple-gateway-default.bpmn";
        var currentElement = "StartEvent";
        var nextElementId = "Task1";
        ProcessReader pr = SetupProcessReader(bpmnfile);
        var actual = pr.GetSequenceFlowsBetween(currentElement, nextElementId);
        var returnedIds = actual.Select(s => s.Id).ToList();
        returnedIds.Should().BeEquivalentTo("Flow1");
        BpmnReader br = SetupBpmnReader(bpmnfile);
        actual.Should().BeEquivalentTo(br.GetSequenceFlowsBetween(currentElement, nextElementId));
    }
    
    [Fact]
    public void GetSequenceFlowsBetween_returns_all_sequenceflows_between_Task1_and_Task2()
    {
        var bpmnfile = "simple-gateway-default.bpmn";
        var currentElement = "Task1";
        var nextElementId = "Task2";
        ProcessReader pr = SetupProcessReader(bpmnfile);
        var actual = pr.GetSequenceFlowsBetween(currentElement, nextElementId);
        var returnedIds = actual.Select(s => s.Id).ToList();
        returnedIds.Should().BeEquivalentTo("Flow2", "Flow3");
        BpmnReader br = SetupBpmnReader(bpmnfile);
        actual.Should().BeEquivalentTo(br.GetSequenceFlowsBetween(currentElement, nextElementId));
    }
    
    [Fact]
    public void GetSequenceFlowsBetween_returns_all_sequenceflows_between_Task1_and_EndEvent()
    {
        var bpmnfile = "simple-gateway-default.bpmn";
        var currentElement = "Task1";
        var nextElementId = "EndEvent";
        ProcessReader pr = SetupProcessReader(bpmnfile);
        var actual = pr.GetSequenceFlowsBetween(currentElement, nextElementId);
        var returnedIds = actual.Select(s => s.Id).ToList();
        returnedIds.Should().BeEquivalentTo("Flow2", "Flow4");
        BpmnReader br = SetupBpmnReader(bpmnfile);
        actual.Should().BeEquivalentTo(br.GetSequenceFlowsBetween(currentElement, nextElementId));
    }

    [Fact]
    public void Constructor_Fails_if_invalid_bpmn()
    {
        Assert.Throws<InvalidOperationException>(() => SetupProcessReader("not-bpmn.bpmn"));
    }

    private ProcessReader SetupProcessReader(string bpmnfile)
    {
        Mock<IProcess> processServiceMock = new Mock<IProcess>();
        var s = new FileStream(Path.Combine(_testDataPath, bpmnfile), FileMode.Open, FileAccess.Read);
        processServiceMock.Setup(p => p.GetProcessDefinition()).Returns(s);
        return new ProcessReader(processServiceMock.Object);
    }

    private BpmnReader SetupBpmnReader(string bpmnfile)
    {
        var s = new FileStream(Path.Combine(_testDataPath, bpmnfile), FileMode.Open, FileAccess.Read);
        return BpmnReader.Create(s);
    }
}

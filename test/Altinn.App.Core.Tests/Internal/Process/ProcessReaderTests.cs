#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using Altinn.App.Core.Internal.Process;
using Altinn.App.Core.Internal.Process.Elements;
using Altinn.App.PlatformServices.Tests.Internal.Process.TestUtils;
using FluentAssertions;
using Xunit;

namespace Altinn.App.PlatformServices.Tests.Internal.Process;

public class ProcessReaderTests
{
    [Fact]
    public void TestBpmnRead()
    {
        ProcessReader pr = ProcessTestUtils.SetupProcessReader("simple-gateway.bpmn");
        BpmnReader br = ProcessTestUtils.SetupBpmnReader("simple-gateway.bpmn");
        pr.GetStartEventIds().Should().Equal("StartEvent").And.Equal(br.StartEvents());
        pr.GetProcessTaskIds().Should().Equal("Task1", "Task2").And.Equal(br.Tasks());
        pr.GetEndEventIds().Should().Equal("EndEvent").And.Equal(br.EndEvents());
        pr.GetSequenceFlowIds().Should().Equal("Flow1", "Flow2", "Flow3", "Flow4", "Flow5");
        pr.GetExclusiveGatewayIds().Should().Equal("Gateway1");
    }

    [Fact]
    public void IsStartEvent_returns_true_when_element_is_StartEvent()
    {
        IProcessReader pr = ProcessTestUtils.SetupProcessReader("simple-gateway.bpmn");
        pr.IsStartEvent("StartEvent").Should().BeTrue();
    }

    [Fact]
    public void IsStartEvent_returns_false_when_element_is_not_StartEvent()
    {
        IProcessReader pr = ProcessTestUtils.SetupProcessReader("simple-gateway.bpmn");
        pr.IsStartEvent("Task1").Should().BeFalse();
        pr.IsStartEvent("EndEvent").Should().BeFalse();
        pr.IsStartEvent("Gateway1").Should().BeFalse();
        pr.IsStartEvent("Foobar").Should().BeFalse();
    }
    
    [Fact]
    public void IsProcessTask_returns_true_when_element_is_ProcessTask()
    {
        IProcessReader pr = ProcessTestUtils.SetupProcessReader("simple-gateway.bpmn");
        pr.IsProcessTask("Task1").Should().BeTrue();
    }

    [Fact]
    public void IsProcessTask_returns_false_when_element_is_not_ProcessTask()
    {
        IProcessReader pr = ProcessTestUtils.SetupProcessReader("simple-gateway.bpmn");
        pr.IsProcessTask("StartEvent").Should().BeFalse();
        pr.IsProcessTask("EndEvent").Should().BeFalse();
        pr.IsProcessTask("Gateway1").Should().BeFalse();
        pr.IsProcessTask("Foobar").Should().BeFalse();
    }
    
    [Fact]
    public void IsEndEvent_returns_true_when_element_is_EndEvent()
    {
        IProcessReader pr = ProcessTestUtils.SetupProcessReader("simple-gateway.bpmn");
        pr.IsEndEvent("EndEvent").Should().BeTrue();
    }

    [Fact]
    public void IsEndEvent_returns_false_when_element_is_not_EndEvent()
    {
        IProcessReader pr = ProcessTestUtils.SetupProcessReader("simple-gateway.bpmn");
        pr.IsEndEvent("StartEvent").Should().BeFalse();
        pr.IsEndEvent("Task1").Should().BeFalse();
        pr.IsEndEvent("Gateway1").Should().BeFalse();
        pr.IsEndEvent("Foobar").Should().BeFalse();
    }
    
    [Fact]
    public void GetNextElement_returns_gateway_if_follow_gateway_false()
    {
        var currentElement = "Task1";
        ProcessReader pr = ProcessTestUtils.SetupProcessReader("simple-gateway.bpmn");
        List<string> nextElements = pr.GetNextElementIds(currentElement, false, false);
        nextElements.Should().Equal("Gateway1");
    }

    [Fact]
    public void GetNextElement_returns_gateways_next_if_follow_gateway_true()
    {
        var bpmnfile = "simple-gateway.bpmn";
        var currentElement = "Task1";
        ProcessReader pr = ProcessTestUtils.SetupProcessReader(bpmnfile);
        List<string> nextElements = pr.GetNextElementIds(currentElement, true, false);
        nextElements.Should().Equal("Task2", "EndEvent");
        BpmnReader br = ProcessTestUtils.SetupBpmnReader(bpmnfile);
        nextElements.Should().Equal(br.NextElements(currentElement, true));
    }

    [Fact]
    public void GetNextElement_returns_gateways_default_next_if_follow_gateway_true_and_use_default()
    {
        var bpmnfile = "simple-gateway-default.bpmn";
        var currentElement = "Task1";
        ProcessReader pr = ProcessTestUtils.SetupProcessReader(bpmnfile);
        List<string> nextElements = pr.GetNextElementIds(currentElement, true, true);
        nextElements.Should().Equal("Task2");
        BpmnReader br = ProcessTestUtils.SetupBpmnReader(bpmnfile);
        nextElements.Should().Equal(br.NextElements(currentElement, false));
    }

    [Fact]
    public void GetNextElement_returns_gateways_next_if_follow_gateway_true_and_use_default_false()
    {
        var bpmnfile = "simple-gateway-default.bpmn";
        var currentElement = "Task1";
        ProcessReader pr = ProcessTestUtils.SetupProcessReader(bpmnfile);
        List<string> nextElements = pr.GetNextElementIds(currentElement, true, false);
        nextElements.Should().Equal("Task2", "EndEvent");
        BpmnReader br = ProcessTestUtils.SetupBpmnReader(bpmnfile);
        nextElements.Should().Equal(br.NextElements(currentElement, true));
    }
    
    [Fact]
    public void GetNextElement_returns_task1_in_simple_process()
    {
        var bpmnfile = "simple-linear.bpmn";
        var currentElement = "StartEvent";
        ProcessReader pr = ProcessTestUtils.SetupProcessReader(bpmnfile);
        List<string> nextElements = pr.GetNextElementIds(currentElement, true, true);
        nextElements.Should().Equal("Task1");
        BpmnReader br = ProcessTestUtils.SetupBpmnReader(bpmnfile);
        nextElements.Should().Equal(br.NextElements(currentElement, false));
    }
    
    [Fact]
    public void GetNextElement_returns_task2_in_simple_process()
    {
        var bpmnfile = "simple-linear.bpmn";
        var currentElement = "Task1";
        ProcessReader pr = ProcessTestUtils.SetupProcessReader(bpmnfile);
        List<string> nextElements = pr.GetNextElementIds(currentElement, true, true);
        nextElements.Should().Equal("Task2");
        BpmnReader br = ProcessTestUtils.SetupBpmnReader(bpmnfile);
        nextElements.Should().Equal(br.NextElements(currentElement, false));
    }
    
    [Fact]
    public void GetNextElement_returns_endevent_in_simple_process()
    {
        var bpmnfile = "simple-linear.bpmn";
        var currentElement = "Task2";
        ProcessReader pr = ProcessTestUtils.SetupProcessReader(bpmnfile);
        List<string> nextElements = pr.GetNextElementIds(currentElement, true, true);
        nextElements.Should().Equal("EndEvent");
        BpmnReader br = ProcessTestUtils.SetupBpmnReader(bpmnfile);
        nextElements.Should().Equal(br.NextElements(currentElement, false));
    }
    
    [Fact]
    public void GetNextElement_returns_emptylist_if_task_without_output()
    {
        var bpmnfile = "simple-no-end.bpmn";
        var currentElement = "Task2";
        ProcessReader pr = ProcessTestUtils.SetupProcessReader(bpmnfile);
        List<string> nextElements = pr.GetNextElementIds(currentElement, true, true);
        nextElements.Should().HaveCount(0);
        BpmnReader br = ProcessTestUtils.SetupBpmnReader(bpmnfile);
        nextElements.Should().Equal(br.NextElements(currentElement, false));
    }
    
    [Fact]
    public void GetNextElement_currentElement_null()
    {
        var bpmnfile = "simple-linear.bpmn";
        ProcessReader pr = ProcessTestUtils.SetupProcessReader(bpmnfile);
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
        BpmnReader br = ProcessTestUtils.SetupBpmnReader(bpmnfile);
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
        ProcessReader pr = ProcessTestUtils.SetupProcessReader(bpmnfile);
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
        BpmnReader br = ProcessTestUtils.SetupBpmnReader(bpmnfile);
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
        ProcessReader pr = ProcessTestUtils.SetupProcessReader(bpmnfile);
        var actual = pr.GetElementInfo(currentElement);
        BpmnReader br = ProcessTestUtils.SetupBpmnReader(bpmnfile);
        var expected = br.GetElementInfo(currentElement);
        actual.Should().BeEquivalentTo(expected);
    }
    
    [Fact]
    public void GetElementInfo_returns_correct_info_for_StartEvent()
    {
        var bpmnfile = "simple-gateway-default.bpmn";
        var currentElement = "StartEvent";
        ProcessReader pr = ProcessTestUtils.SetupProcessReader(bpmnfile);
        var actual = pr.GetElementInfo(currentElement);
        BpmnReader br = ProcessTestUtils.SetupBpmnReader(bpmnfile);
        var expected = br.GetElementInfo(currentElement);
        actual.Should().BeEquivalentTo(expected);
    }
    
    [Fact]
    public void GetElementInfo_returns_correct_info_for_EndEvent()
    {
        var bpmnfile = "simple-gateway-default.bpmn";
        var currentElement = "EndEvent";
        ProcessReader pr = ProcessTestUtils.SetupProcessReader(bpmnfile);
        var actual = pr.GetElementInfo(currentElement);
        BpmnReader br = ProcessTestUtils.SetupBpmnReader(bpmnfile);
        var expected = br.GetElementInfo(currentElement);
        actual.Should().BeEquivalentTo(expected);
    }
    
    [Fact]
    public void GetElementInfo_returns_null_for_ExclusiveGateway()
    {
        var bpmnfile = "simple-gateway-default.bpmn";
        var currentElement = "Gateway1";
        ProcessReader pr = ProcessTestUtils.SetupProcessReader(bpmnfile);
        var actual = pr.GetElementInfo(currentElement);
        BpmnReader br = ProcessTestUtils.SetupBpmnReader(bpmnfile);
        var expected = br.GetElementInfo(currentElement);
        actual.Should().BeEquivalentTo(expected);
    }
    
    [Fact]
    public void GetElementInfo_throws_argument_null_expcetion_when_elementName_is_null()
    {
        var bpmnfile = "simple-linear.bpmn";
        ProcessReader pr = ProcessTestUtils.SetupProcessReader(bpmnfile);
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
        BpmnReader br = ProcessTestUtils.SetupBpmnReader(bpmnfile);
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
    public void GetOutgoingSequenceFlows_returns_empty_list_if_input_is_null()
    {
        var bpmnfile = "simple-gateway-default.bpmn";
        ProcessReader pr = ProcessTestUtils.SetupProcessReader(bpmnfile);
        pr.GetOutgoingSequenceFlows(null).Should().BeEmpty();
    }

    [Fact]
    public void GetOutgoingSequenceFlows_returns_SequenceFlow_objects_for_outgoing_flows_from_ProcessTask()
    {
        var bpmnfile = "simple-gateway-default.bpmn";
        ProcessReader pr = ProcessTestUtils.SetupProcessReader(bpmnfile);
        List<SequenceFlow> outgoingFLows = pr.GetOutgoingSequenceFlows(pr.GetFlowElement("Task1"));
        outgoingFLows.Should().BeEquivalentTo(new List<SequenceFlow>
        {
            new SequenceFlow()
            {
                Id = "Flow2",
                FlowType = null!,
                SourceRef = "Task1",
                TargetRef = "Gateway1"
            }
        });
    }
    
    [Fact]
    public void GetOutgoingSequenceFlows_returns_SequenceFlow_objects_for_outgoing_flows_from_Gateway()
    {
        var bpmnfile = "simple-gateway-default.bpmn";
        ProcessReader pr = ProcessTestUtils.SetupProcessReader(bpmnfile);
        List<SequenceFlow> outgoingFLows = pr.GetOutgoingSequenceFlows(pr.GetFlowElement("Gateway1"));
        outgoingFLows.Should().BeEquivalentTo(new List<SequenceFlow>
        {
            new SequenceFlow()
            {
                Id = "Flow3",
                FlowType = null!,
                SourceRef = "Gateway1",
                TargetRef = "Task2"
            },
            new SequenceFlow()
            {
                Id = "Flow4",
                FlowType = null!,
                SourceRef = "Gateway1",
                TargetRef = "EndEvent"
            }
        });
    }
    
    [Fact]
    public void GetOutgoingSequenceFlows_returns_empty_list_when_no_outgoing()
    {
        var bpmnfile = "simple-gateway-default.bpmn";
        ProcessReader pr = ProcessTestUtils.SetupProcessReader(bpmnfile);
        List<SequenceFlow> outgoingFLows = pr.GetOutgoingSequenceFlows(pr.GetFlowElement("EndEvent"));
        outgoingFLows.Should().BeEmpty();
    }
    
    [Fact]
    public void GetSequenceFlowsBetween_returns_all_sequenceflows_between_StartEvent_and_Task1()
    {
        var bpmnfile = "simple-gateway-default.bpmn";
        var currentElement = "StartEvent";
        var nextElementId = "Task1";
        ProcessReader pr = ProcessTestUtils.SetupProcessReader(bpmnfile);
        var actual = pr.GetSequenceFlowsBetween(currentElement, nextElementId);
        var returnedIds = actual.Select(s => s.Id).ToList();
        returnedIds.Should().BeEquivalentTo("Flow1");
        BpmnReader br = ProcessTestUtils.SetupBpmnReader(bpmnfile);
        actual.Should().BeEquivalentTo(br.GetSequenceFlowsBetween(currentElement, nextElementId));
    }
    
    [Fact]
    public void GetSequenceFlowsBetween_returns_all_sequenceflows_between_Task1_and_Task2()
    {
        var bpmnfile = "simple-gateway-default.bpmn";
        var currentElement = "Task1";
        var nextElementId = "Task2";
        ProcessReader pr = ProcessTestUtils.SetupProcessReader(bpmnfile);
        var actual = pr.GetSequenceFlowsBetween(currentElement, nextElementId);
        var returnedIds = actual.Select(s => s.Id).ToList();
        returnedIds.Should().BeEquivalentTo("Flow2", "Flow3");
        BpmnReader br = ProcessTestUtils.SetupBpmnReader(bpmnfile);
        actual.Should().BeEquivalentTo(br.GetSequenceFlowsBetween(currentElement, nextElementId));
    }
    
    [Fact]
    public void GetSequenceFlowsBetween_returns_all_sequenceflows_between_Task1_and_EndEvent()
    {
        var bpmnfile = "simple-gateway-default.bpmn";
        var currentElement = "Task1";
        var nextElementId = "EndEvent";
        ProcessReader pr = ProcessTestUtils.SetupProcessReader(bpmnfile);
        var actual = pr.GetSequenceFlowsBetween(currentElement, nextElementId);
        var returnedIds = actual.Select(s => s.Id).ToList();
        returnedIds.Should().BeEquivalentTo("Flow2", "Flow4");
        BpmnReader br = ProcessTestUtils.SetupBpmnReader(bpmnfile);
        actual.Should().BeEquivalentTo(br.GetSequenceFlowsBetween(currentElement, nextElementId));
    }
    
    [Fact]
    public void GetSequenceFlowsBetween_returns_all_sequenceflows_between_Task1_and_EndEvent_complex()
    {
        var bpmnfile = "simple-gateway-with-join-gateway.bpmn";
        var currentElement = "Task1";
        var nextElementId = "EndEvent";
        ProcessReader pr = ProcessTestUtils.SetupProcessReader(bpmnfile);
        var actual = pr.GetSequenceFlowsBetween(currentElement, nextElementId);
        var returnedIds = actual.Select(s => s.Id).ToList();
        returnedIds.Should().BeEquivalentTo("Flow2", "Flow4", "Flow6");
        BpmnReader br = ProcessTestUtils.SetupBpmnReader(bpmnfile);
        actual.Should().BeEquivalentTo(br.GetSequenceFlowsBetween(currentElement, nextElementId));
    }
    
    [Fact]
    public void GetSequenceFlowsBetween_returns_empty_list_when_unknown_target()
    {
        var bpmnfile = "simple-gateway-default.bpmn";
        var currentElement = "Task1";
        var nextElementId = "Foobar";
        ProcessReader pr = ProcessTestUtils.SetupProcessReader(bpmnfile);
        var actual = pr.GetSequenceFlowsBetween(currentElement, nextElementId);
        var returnedIds = actual.Select(s => s.Id).ToList();
        returnedIds.Should().BeEmpty();
        BpmnReader br = ProcessTestUtils.SetupBpmnReader(bpmnfile);
        actual.Should().BeEquivalentTo(br.GetSequenceFlowsBetween(currentElement, nextElementId));
    }
    
    [Fact]
    public void GetSequenceFlowsBetween_returns_empty_list_when_current_is_null()
    {
        var bpmnfile = "simple-gateway-default.bpmn";
        string? currentElement = null;
        var nextElementId = "Foobar";
        ProcessReader pr = ProcessTestUtils.SetupProcessReader(bpmnfile);
        var actual = pr.GetSequenceFlowsBetween(currentElement, nextElementId);
        var returnedIds = actual.Select(s => s.Id).ToList();
        returnedIds.Should().BeEmpty();
        BpmnReader br = ProcessTestUtils.SetupBpmnReader(bpmnfile);
        actual.Should().BeEquivalentTo(br.GetSequenceFlowsBetween(currentElement, nextElementId));
    }
    
    [Fact]
    public void GetSequenceFlowsBetween_returns_empty_list_when_next_is_null()
    {
        var bpmnfile = "simple-gateway-default.bpmn";
        string? currentElement = "Task1";
        string? nextElementId = null;
        ProcessReader pr = ProcessTestUtils.SetupProcessReader(bpmnfile);
        var actual = pr.GetSequenceFlowsBetween(currentElement, nextElementId);
        var returnedIds = actual.Select(s => s.Id).ToList();
        returnedIds.Should().BeEmpty();
        BpmnReader br = ProcessTestUtils.SetupBpmnReader(bpmnfile);
        actual.Should().BeEquivalentTo(br.GetSequenceFlowsBetween(currentElement, nextElementId));
    }
    
    [Fact]
    public void GetSequenceFlowsBetween_returns_empty_list_when_current_and_next_is_null()
    {
        var bpmnfile = "simple-gateway-default.bpmn";
        string? currentElement = null;
        string? nextElementId = null;
        ProcessReader pr = ProcessTestUtils.SetupProcessReader(bpmnfile);
        var actual = pr.GetSequenceFlowsBetween(currentElement, nextElementId);
        var returnedIds = actual.Select(s => s.Id).ToList();
        returnedIds.Should().BeEmpty();
        BpmnReader br = ProcessTestUtils.SetupBpmnReader(bpmnfile);
        actual.Should().BeEquivalentTo(br.GetSequenceFlowsBetween(currentElement, nextElementId));
    }

    [Fact]
    public void Constructor_Fails_if_invalid_bpmn()
    {
        Assert.Throws<InvalidOperationException>(() => ProcessTestUtils.SetupProcessReader("not-bpmn.bpmn"));
    }

    [Fact]
    public void GetFlowElement_returns_StartEvent_with_id()
    {
        var bpmnfile = "simple-gateway-default.bpmn";
        ProcessReader pr = ProcessTestUtils.SetupProcessReader(bpmnfile);
        pr.GetFlowElement("StartEvent").Should().BeOfType<StartEvent>().And.BeEquivalentTo(new StartEvent()
        {
            Id = "StartEvent",
            Name = null!,
            Incoming = new List<string>(),
            Outgoing = new List<string> { "Flow1" }
        });
    }
    
    [Fact]
    public void GetFlowElement_returns_ProcessTask_with_id()
    {
        var bpmnfile = "simple-gateway-default.bpmn";
        ProcessReader pr = ProcessTestUtils.SetupProcessReader(bpmnfile);
        pr.GetFlowElement("Task1").Should().BeOfType<ProcessTask>().And.BeEquivalentTo(new ProcessTask()
        {
            Id = "Task1",
            Name = null!,
            Incoming = new List<string> { "Flow1" },
            Outgoing = new List<string> { "Flow2" }
        });
    }
    
    [Fact]
    public void GetFlowElement_returns_EndEvent_with_id()
    {
        var bpmnfile = "simple-gateway-default.bpmn";
        ProcessReader pr = ProcessTestUtils.SetupProcessReader(bpmnfile);
        pr.GetFlowElement("EndEvent").Should().BeOfType<EndEvent>().And.BeEquivalentTo(new EndEvent()
        {
            Id = "EndEvent",
            Name = null!,
            Incoming = new List<string> { "Flow4", "Flow5" },
            Outgoing = new List<string>()
        });
    }
    
    [Fact]
    public void GetFlowElement_returns_null_when_id_not_found()
    {
        var bpmnfile = "simple-gateway-default.bpmn";
        ProcessReader pr = ProcessTestUtils.SetupProcessReader(bpmnfile);
        pr.GetFlowElement("Foobar").Should().BeNull();
    }
    
    [Fact]
    public void GetFlowElement_returns_Gateway_with_id()
    {
        var bpmnfile = "simple-gateway-default.bpmn";
        ProcessReader pr = ProcessTestUtils.SetupProcessReader(bpmnfile);
        pr.GetFlowElement("Gateway1").Should().BeOfType<ExclusiveGateway>().And.BeEquivalentTo(new ExclusiveGateway()
        {
            Id = "Gateway1",
            Name = null!,
            Default = "Flow3",
            Incoming = new List<string> { "Flow2" },
            Outgoing = new List<string> { "Flow3", "Flow4" }
        });
    }
}

using Altinn.App.Core.Models.Process;
using Altinn.Platform.Storage.Interface.Models;
using Prometheus;

namespace Altinn.App.Core.Internal.Process
{
    public class ProcessEngineMetricsDecorator : IProcessEngine
    {
        private readonly IProcessEngine _processEngine;
        private static readonly Counter ProcessTaskStartCounter = Metrics.CreateCounter("altinn_app_process_start_count", "Number of tasks started", labelNames: new []{ "result" });
        private static readonly Counter ProcessTaskNextCounter = Metrics.CreateCounter("altinn_app_process_task_next_count", "Number of tasks moved to next", labelNames: new []{ "result", "action", "task" });
        private static readonly Counter ProcessTaskEndCounter = Metrics.CreateCounter("altinn_app_process_end_count", "Number of tasks ended", labelNames: new []{ "result" });
        private static readonly Counter ProcessTimeCounter = Metrics.CreateCounter("altinn_app_process_end_time_total", "Number of seconds used to complete instances", labelNames: new []{ "result" });

        public ProcessEngineMetricsDecorator(IProcessEngine processEngine)
        {
            _processEngine = processEngine;
        }


        public async Task<ProcessChangeResult> StartProcess(ProcessStartRequest processStartRequest)
        {
            var result = await _processEngine.StartProcess(processStartRequest);
            ProcessTaskStartCounter.WithLabels(result.Success ? "success" : "failure").Inc();
            return result;
        }

        public async Task<ProcessChangeResult> Next(ProcessNextRequest request)
        {
            var result = await _processEngine.Next(request);
            ProcessTaskNextCounter.WithLabels(result.Success ? "success" : "failure", request.Action?? "", request.Instance.Process?.CurrentTask?.ElementId ?? "").Inc();
            if(result.ProcessStateChange?.NewProcessState?.Ended != null)
            {
                ProcessTaskEndCounter.WithLabels(result.Success ? "success" : "failure").Inc();
                if (result.ProcessStateChange?.NewProcessState?.Started != null)
                {
                    ProcessTimeCounter.WithLabels(result.Success ? "success" : "failure").Inc(result.ProcessStateChange.NewProcessState.Ended.Value.Subtract(result.ProcessStateChange.NewProcessState.Started.Value).TotalSeconds);
                }
            }
            return result;
        }

        public async Task<Instance> UpdateInstanceAndRerunEvents(ProcessStartRequest startRequest, List<InstanceEvent>? events)
        {
            return await _processEngine.UpdateInstanceAndRerunEvents(startRequest, events);
        }
    }
}

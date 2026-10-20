# Preview PDFs for a service task (v8)

The preview endpoint can render a PDF task before the instance reaches it:

```http
GET /{org}/{app}/instances/{partyId}/{instanceGuid}/pdf/preview
GET /{org}/{app}/instances/{partyId}/{instanceGuid}/pdf/preview?taskId={pdfTaskId}
GET /{org}/{app}/instances/{partyId}/{instanceGuid}/pdf/preview?taskId={subformPdfTaskId}&dataElementId={subformDataElementId}
```

`language` is optional and defaults to the user's language. Without `taskId`, the
endpoint keeps the existing v8 current-task preview behavior.

An explicit `taskId` must identify a `pdf` or `subformPdf` service task. The preview
uses its PDF configuration, including `autoPdfTaskIds`. A subform preview requires
an existing data element of the configured subform data type. It renders only that
subform. Generating a preview does not store a PDF or advance the process.

Explicit-task previews require frontend v4 with the corresponding preview support:
it renders the requested task and subform even when another task is current, and
prevents that other task's layouts from modifying form data. Deploy the frontend
support before using the new query parameters.

Explicit-task previews share a limit of 20 requests per minute per running app.
Configure `PdfGeneratorSettings:PreviewRateLimit` (`PermitLimit`, `Window`,
`QueueLimit`) to adjust it; `PermitLimit: 0` disables it. Excess requests receive
HTTP 429 with `Retry-After`. Existing previews without `taskId` are not limited.

The public `IPdfService` and `IPdfGeneratorClient` contracts, constructors, stream
results, stored-PDF behavior, file names, telemetry, and legacy PDF formatting
remain compatible with v8. The new rendering entry point is internal.

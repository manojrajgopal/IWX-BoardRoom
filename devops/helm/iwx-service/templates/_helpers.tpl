{{/*
Resolve the service name. Prefer nameOverride, fall back to release name.
*/}}
{{- define "iwx-service.name" -}}
{{- default .Release.Name .Values.nameOverride | trunc 63 | trimSuffix "-" -}}
{{- end -}}

{{- define "iwx-service.fullname" -}}
{{- include "iwx-service.name" . -}}
{{- end -}}

{{- define "iwx-service.labels" -}}
app.kubernetes.io/name: {{ include "iwx-service.name" . }}
app.kubernetes.io/instance: {{ .Release.Name }}
app.kubernetes.io/managed-by: {{ .Release.Service }}
iwx.platform/component: {{ include "iwx-service.name" . }}
{{- end -}}

{{- define "iwx-service.selectorLabels" -}}
app.kubernetes.io/name: {{ include "iwx-service.name" . }}
app.kubernetes.io/instance: {{ .Release.Name }}
{{- end -}}

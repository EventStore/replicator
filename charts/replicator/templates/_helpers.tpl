{{/* vim: set filetype=mustache: */}}
{{/*
Expand the name of the chart.
*/}}
{{- define "replicator.name" -}}
{{- default .Chart.Name .Values.nameOverride | trunc 63 | trimSuffix "-" -}}
{{- end -}}

{{/*
Create a default fully qualified app name.
If release name contains chart name it will be used as a full name.
*/}}
{{- define "replicator.fullname" -}}
{{- $name := .Chart.Name -}}
{{- if contains $name .Release.Name -}}
{{- .Release.Name | trunc 63 | trimSuffix "-" -}}
{{- else -}}
{{- printf "%s-%s" .Release.Name $name | trunc 63 | trimSuffix "-" -}}
{{- end -}}
{{- end -}}

{{/*
Create chart name and version as used by the chart label.
*/}}
{{- define "replicator.chart" -}}
{{- printf "%s-%s" .Chart.Name .Chart.Version | replace "+" "_" | trunc 63 | trimSuffix "-" -}}
{{- end -}}

{{/*
Return the appropriate apiVersion for deployment.
*/}}
{{- define "deployment.apiVersion" -}}
{{- if semverCompare ">=1.9-0" .Capabilities.KubeVersion.GitVersion -}}
{{- print "apps/v1" -}}
{{- else -}}
{{- print "extensions/v1beta1" -}}
{{- end -}}
{{- end -}}

{{/*
Return the proper Replicator image name
*/}}
{{- define "replicator.image" -}}
{{- $registryName := .Values.image.registry -}}
{{- $repositoryName := .Values.image.repository -}}
{{- $tag := .Values.image.tag | toString -}}
{{- printf "%s/%s:%s" $registryName $repositoryName $tag -}}
{{- end -}}

{{/*
Checks if replicator is configured to use javascript transform
*/}}
{{- define "replicator.shouldUseJavascriptTransform" -}}
{{- if and 
    .Values.replicator.transform 
    (eq .Values.replicator.transform.type "js") 
    .Values.replicator.transform.config 
    .Values.transformJs 
-}}
{{- print "true" }}
{{- end -}}
{{- end -}}

{{/*
Checks if replicator is configured to use a custom partitioner
*/}}
{{- define "replicator.shouldUseCustomPartitioner" -}}
{{- if and 
    .Values.replicator.sink.partitioner 
    .Values.partitionerJs 
-}}
{{- print "true" }}
{{- end -}}
{{- end -}}

{{- define "replicator.transform.filename" -}}
{{- if eq (include "replicator.shouldUseJavascriptTransform" .) "true" -}}
{{ printf "%s" (include "replicator.helpers.filename" .Values.replicator.transform.config) }}
{{- end -}}
{{- end -}}

{{- define "replicator.transform.filepath" -}}
{{- if eq (include "replicator.shouldUseJavascriptTransform" .) "true" -}}
{{ printf "%s" (include "replicator.helpers.cleansePath" .Values.replicator.transform.config) }}
{{- end -}}
{{- end -}}

{{- define "replicator.sink.partitioner.filename" -}}
{{- if eq (include "replicator.shouldUseCustomPartitioner" .) "true" -}}
{{ printf "%s" (include "replicator.helpers.filename" .Values.replicator.sink.partitioner) }}
{{- end -}}
{{- end -}}

{{- define "replicator.sink.partitioner.filepath" -}}
{{- if eq (include "replicator.shouldUseCustomPartitioner" .) "true" -}}
{{ printf "%s" (include "replicator.helpers.cleansePath" .Values.replicator.sink.partitioner) }}
{{- end -}}
{{- end -}}

{{- define "replicator.helpers.filename" -}}
{{- $file := . -}}
{{- $filename := last (splitList "/" $file) -}}
{{- $filename -}}
{{- end -}}

{{- define "replicator.helpers.cleansePath" -}}
{{- $path := . -}}
{{- $path = replace "./" "/" $path -}}
{{- if not (hasPrefix "/" $path) -}}
{{- $path = printf "/%s" $path -}}
{{- end -}}
{{- $path -}}
{{- end -}}
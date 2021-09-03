package main

import (
	"bufio"
	"fmt"
	"io/ioutil"
	"os"
	"path"
	"reflect"
	"strconv"
	"strings"

	"github.com/docker/docker/api/types"
	"github.com/docker/docker/api/types/container"
	"github.com/docker/docker/api/types/events"
	"github.com/docker/docker/api/types/image"
	"github.com/docker/docker/api/types/network"
	"github.com/docker/docker/api/types/registry"
	"github.com/docker/docker/api/types/swarm"
	"github.com/docker/docker/api/types/swarm/runtime"
	"github.com/docker/docker/pkg/jsonmessage"
)

var reflectedTypes = map[string]*CSModelType{}

func typeToKey(t reflect.Type) string {
	return t.String()
}

var typesToDisambiguate = map[string]*CSModelType{
	typeToKey(reflect.TypeOf(container.Config{})): {
		Properties: []CSProperty{
			CSProperty{
				Name:       "StopTimeout",
				Type:       CSType{"System", "TimeSpan", true},
				Attributes: []CSAttribute{CSAttribute{Type: CSType{"Newtonsoft.Json", "JsonConverter", false}, Arguments: []CSArgument{{Value: "typeof(TimeSpanSecondsConverter)"}}}},
			},
		},
	},
	typeToKey(reflect.TypeOf(container.ContainerCreateCreatedBody{})): {Name: "CreateContainerResponse"},
	typeToKey(reflect.TypeOf(container.HealthConfig{})): {
		Properties: []CSProperty{
			CSProperty{
				Name:       "Interval",
				Type:       CSType{"System", "TimeSpan", true},
				Attributes: []CSAttribute{CSAttribute{Type: CSType{"Newtonsoft.Json", "JsonConverter", false}, Arguments: []CSArgument{{Value: "typeof(TimeSpanNanosecondsConverter)"}}}},
			},
			CSProperty{
				Name:       "Timeout",
				Type:       CSType{"System", "TimeSpan", true},
				Attributes: []CSAttribute{CSAttribute{Type: CSType{"Newtonsoft.Json", "JsonConverter", false}, Arguments: []CSArgument{{Value: "typeof(TimeSpanSecondsConverter)"}}}},
			},
		},
	},
	typeToKey(reflect.TypeOf(container.RestartPolicy{})): {
		Properties: []CSProperty{CSProperty{Name: "Name", Type: CSType{"", "RestartPolicyKind", false}}},
	},
	typeToKey(reflect.TypeOf(jsonmessage.JSONMessage{})): {
		Properties: []CSProperty{
			CSProperty{Name: "Time", Type: CSType{"System", "DateTime", false}},
			CSProperty{Name: "Aux", Type: CSType{"", "ObjectExtensionData", false}},
		},
	},
	typeToKey(reflect.TypeOf(CreateContainerParameters{})): {
		Properties: []CSProperty{
			CSProperty{
				Name:       "StopTimeout",
				Type:       CSType{"System", "TimeSpan", true},
				Attributes: []CSAttribute{CSAttribute{Type: CSType{"Newtonsoft.Json", "JsonConverter", false}, Arguments: []CSArgument{{Value: "typeof(TimeSpanSecondsConverter)"}}}},
			},
		},
	},
	typeToKey(reflect.TypeOf(network.Task{})):                {Name: "NetworkTask"},
	typeToKey(reflect.TypeOf(registry.AuthenticateOKBody{})): {Name: "AuthResponse"},
	typeToKey(reflect.TypeOf(registry.SearchResult{})):       {Name: "ImageSearchResponse"},
	typeToKey(reflect.TypeOf(runtime.PluginPrivilege{})):     {Name: "RuntimePluginPrivilege"},
	typeToKey(reflect.TypeOf(swarm.ConfigSpec{})):            {Name: "SwarmConfigSpec"},
	typeToKey(reflect.TypeOf(swarm.Driver{})):                {Name: "SwarmDriver"},
	typeToKey(reflect.TypeOf(swarm.InitRequest{})):           {Name: "SwarmInitParameters"},
	typeToKey(reflect.TypeOf(swarm.JoinRequest{})):           {Name: "SwarmJoinParameters"},
	typeToKey(reflect.TypeOf(swarm.IPAMConfig{})):            {Name: "SwarmIPAMConfig"},
	typeToKey(reflect.TypeOf(swarm.Limit{})):                 {Name: "SwarmLimit"},
	typeToKey(reflect.TypeOf(swarm.Node{})):                  {Name: "NodeListResponse"},
	typeToKey(reflect.TypeOf(swarm.NodeSpec{})):              {Name: "NodeUpdateParameters"},
	typeToKey(reflect.TypeOf(swarm.RestartPolicy{})):         {Name: "SwarmRestartPolicy"},
	typeToKey(reflect.TypeOf(swarm.Resources{})):             {Name: "SwarmResources"},
	typeToKey(reflect.TypeOf(swarm.Service{})):               {Name: "SwarmService"},
	typeToKey(reflect.TypeOf(swarm.Swarm{})):                 {Name: "SwarmInspectResponse"},
	typeToKey(reflect.TypeOf(swarm.Task{})): {
		Name: "TaskResponse",
		Properties: []CSProperty{
			CSProperty{Name: "DesiredState", Type: CSType{"", "TaskState", false}},
		},
	},
	typeToKey(reflect.TypeOf(swarm.TaskStatus{})): {
		Properties: []CSProperty{
			CSProperty{Name: "State", Type: CSType{"", "TaskState", false}},
		},
	},
	typeToKey(reflect.TypeOf(swarm.UpdateConfig{})):    {Name: "SwarmUpdateConfig"},
	typeToKey(reflect.TypeOf(swarm.ConfigReference{})): {Name: "SwarmConfigReference"},
	typeToKey(reflect.TypeOf(types.Container{})): {
		Name: "ContainerListResponse",
		Properties: []CSProperty{
			CSProperty{Name: "Created", Type: CSType{"System", "DateTime", false}},
		},
	},
	typeToKey(reflect.TypeOf(container.ContainerChangeResponseItem{})): {
		Name: "ContainerFileSystemChangeResponse",
		Properties: []CSProperty{
			CSProperty{Name: "Kind", Type: CSType{"", "FileSystemChangeKind", false}},
		},
	},
	typeToKey(reflect.TypeOf(types.ContainerExecInspect{})): {Name: "ContainerExecInspectResponse"},
	typeToKey(reflect.TypeOf(types.ContainerJSON{})):        {Name: "ContainerInspectResponse"},
	typeToKey(reflect.TypeOf(types.ContainerJSONBase{})): {
		Properties: []CSProperty{
			CSProperty{Name: "Created", Type: CSType{"System", "DateTime", false}},
		},
	},
	typeToKey(reflect.TypeOf(types.ContainerPathStat{})):       {Name: "ContainerPathStatResponse"},
	typeToKey(reflect.TypeOf(container.ContainerTopOKBody{})):  {Name: "ContainerProcessesResponse"},
	typeToKey(reflect.TypeOf(types.ContainersPruneReport{})):   {Name: "ContainersPruneResponse"},
	typeToKey(reflect.TypeOf(types.ImageDeleteResponseItem{})): {Name: "ImageDeleteResponse"},
	typeToKey(reflect.TypeOf(image.HistoryResponseItem{})): {
		Name: "ImageHistoryResponse",
		Properties: []CSProperty{
			CSProperty{Name: "Created", Type: CSType{"System", "DateTime", false}},
		},
	},
	typeToKey(reflect.TypeOf(types.ImageInspect{})): {
		Name: "ImageInspectResponse",
		Properties: []CSProperty{
			CSProperty{Name: "Created", Type: CSType{"System", "DateTime", false}},
		},
	},
	typeToKey(reflect.TypeOf(types.ImageLoadResponse{})): {Name: "ImagesLoadResponse"},
	typeToKey(reflect.TypeOf(types.ImagesPruneReport{})): {Name: "ImagesPruneResponse"},
	typeToKey(reflect.TypeOf(types.ImageSummary{})): {
		Name: "ImagesListResponse",
		Properties: []CSProperty{
			CSProperty{Name: "Created", Type: CSType{"System", "DateTime", false}},
		},
	},
	typeToKey(reflect.TypeOf(types.Info{})):                  {Name: "SystemInfoResponse"},
	typeToKey(reflect.TypeOf(types.NetworkConnect{})):        {Name: "NetworkConnectParameters"},
	typeToKey(reflect.TypeOf(types.NetworkCreateRequest{})):  {Name: "NetworksCreateParameters"},
	typeToKey(reflect.TypeOf(types.NetworkCreateResponse{})): {Name: "NetworksCreateResponse"},
	typeToKey(reflect.TypeOf(types.NetworkDisconnect{})):     {Name: "NetworkDisconnectParameters"},
	typeToKey(reflect.TypeOf(types.NetworksPruneReport{})):   {Name: "NetworksPruneResponse"},
	typeToKey(reflect.TypeOf(types.NetworkResource{})):       {Name: "NetworkResponse"},
	typeToKey(reflect.TypeOf(types.PluginConfigInterface{})): {
		Name: "PluginConfigInterface",
		Properties: []CSProperty{
			CSProperty{Name: "Types", Type: CSType{"System.Collections.Generic", "IList<string>", false}},
		},
	},
	typeToKey(reflect.TypeOf(types.StatsJSON{})):          {Name: "ContainerStatsResponse"},
	typeToKey(reflect.TypeOf(types.Version{})):            {Name: "VersionResponse"},
	typeToKey(reflect.TypeOf(types.VolumesPruneReport{})): {Name: "VolumesPruneResponse"},
	typeToKey(reflect.TypeOf(VolumeResponse{})):           {Name: "VolumeResponse"},
}

var dockerTypesToReflect = []reflect.Type{

	// POST /auth
	reflect.TypeOf(types.AuthConfig{}),
	reflect.TypeOf(registry.AuthenticateOKBody{}),

	// POST /build
	reflect.TypeOf(ImageBuildParameters{}),
	reflect.TypeOf(types.ImageBuildResponse{}),

	// POST /commit
	reflect.TypeOf(CommitContainerChangesParameters{}),
	reflect.TypeOf(CommitContainerChangesResponse{}),

	// POST /containers/create
	reflect.TypeOf(CreateContainerParameters{}),
	reflect.TypeOf(container.ContainerCreateCreatedBody{}),

	// GET /containers/json
	reflect.TypeOf(ContainersListParameters{}),
	reflect.TypeOf(types.Container{}),

	// POST /containers/prune
	reflect.TypeOf(ContainersPruneParameters{}),
	reflect.TypeOf(types.ContainersPruneReport{}),

	// DELETE /containers/(id)
	reflect.TypeOf(ContainerRemoveParameters{}),

	// GET /containers/(id)/archive
	reflect.TypeOf(ContainerPathStatParameters{}),
	reflect.TypeOf(types.ContainerPathStat{}),

	// POST /containers/(id)/attach
	reflect.TypeOf(ContainerAttachParameters{}),

	// POST /containers/(id)/attach/ws

	// GET /containers/(id)/changes
	reflect.TypeOf(container.ContainerChangeResponseItem{}),

	// OBSOLETE - POST /containers/(id)/copy

	// GET /containers/(id)/export
	// TODO: TAR Stream

	// POST /containers/(id)/exec
	reflect.TypeOf(ContainerExecCreateParameters{}),
	reflect.TypeOf(ContainerExecCreateResponse{}),

	// GET /containers/(id)/json
	reflect.TypeOf(ContainerInspectParameters{}),
	reflect.TypeOf(types.ContainerJSON{}),

	// POST /containers/(id)/kill
	reflect.TypeOf(ContainerKillParameters{}),

	// GET /containers/(id)/logs
	reflect.TypeOf(ContainerLogsParameters{}),

	// POST /containers/(id)/pause

	// POST /containers/(id)/rename
	reflect.TypeOf(ContainerRenameParameters{}),

	// POST /containers/(id)/resize
	// POST /exec/(id)/resize
	reflect.TypeOf(ContainerResizeParameters{}),

	// POST /containers/(id)/restart
	reflect.TypeOf(ContainerRestartParameters{}),

	// POST /containers/(id)/start
	reflect.TypeOf(ContainerStartParameters{}),

	// POST /containers/(id)/stop
	reflect.TypeOf(ContainerStopParameters{}),

	// GET /containers/(id)/stats
	reflect.TypeOf(ContainerStatsParameters{}),
	reflect.TypeOf(types.StatsJSON{}),

	// GET /containers/(id)/top
	reflect.TypeOf(ContainerListProcessesParameters{}),
	reflect.TypeOf(container.ContainerTopOKBody{}),

	// POST /containers/(id)/unpause

	// POST /containers/(id)/update
	reflect.TypeOf(ContainerUpdateParameters{}),
	reflect.TypeOf(ContainerUpdateResponse{}),

	// POST /containers/(id)/wait
	reflect.TypeOf(ContainerWaitResponse{}),

	// POST /exec/(id)/start
	reflect.TypeOf(ContainerExecStartParameters{}),

	// GET /exec/(id)/json
	reflect.TypeOf(types.ContainerExecInspect{}),

	// GET /events
	reflect.TypeOf(ContainerEventsParameters{}),
	reflect.TypeOf(events.Actor{}),
	reflect.TypeOf(events.Message{}),

	// POST /images/create
	reflect.TypeOf(ImagesCreateParameters{}),
	reflect.TypeOf(jsonmessage.JSONMessage{}),

	// GET /images/get
	// TODO: stream

	// GET /images/json
	reflect.TypeOf(ImagesListParameters{}),
	reflect.TypeOf(types.ImageSummary{}),

	// POST /images/load
	// TODO: headers: application/x-tar body.
	reflect.TypeOf(ImageLoadParameters{}),
	reflect.TypeOf(types.ImageLoadResponse{}),

	// POST /images/prune
	reflect.TypeOf(ImagesPruneParameters{}),
	reflect.TypeOf(types.ImagesPruneReport{}),

	// GET /images/search
	reflect.TypeOf(ImagesSearchParameters{}),
	reflect.TypeOf(registry.SearchResult{}),

	// DELETE /images/(id)
	reflect.TypeOf(ImageDeleteParameters{}),
	reflect.TypeOf(types.ImageDeleteResponseItem{}),

	// GET /images/(id)/history
	reflect.TypeOf(image.HistoryResponseItem{}),

	// GET /images/(id)/json
	reflect.TypeOf(ImageInspectParameters{}),
	reflect.TypeOf(types.ImageInspect{}),

	// POST /images/(id)/push
	reflect.TypeOf(ImagePushParameters{}),

	// POST /images/(id)/tag
	reflect.TypeOf(ImageTagParameters{}),

	// GET /info
	reflect.TypeOf(types.Info{}),
	reflect.TypeOf(registry.ServiceConfig{}),

	// GET /networks
	reflect.TypeOf(NetworksListParameters{}),
	reflect.TypeOf(types.NetworkResource{}),

	// POST /networks/create
	reflect.TypeOf(types.NetworkCreateRequest{}),
	reflect.TypeOf(types.NetworkCreateResponse{}),

	// POST /networks/prune
	reflect.TypeOf(NetworksDeleteUnusedParameters{}),
	reflect.TypeOf(types.NetworksPruneReport{}),

	// GET /networks/(id)
	// []NetworkResponse reflected above in GET /networks

	// DELETE /networks/(id)

	// POST /networks/(id)/connect
	reflect.TypeOf(types.NetworkConnect{}),

	// POST /networks/(id)/disconnect
	reflect.TypeOf(types.NetworkDisconnect{}),

	// GET /plugins
	// []Plugin
	reflect.TypeOf(PluginListParameters{}),
	reflect.TypeOf(types.Plugin{}),

	// GET /plugins/privileges
	// []PluginPrivilege
	reflect.TypeOf(PluginGetPrivilegeParameters{}),
	reflect.TypeOf(types.PluginPrivilege{}),

	// POST /plugins/pull
	// []PluginConfigArgs
	reflect.TypeOf(PluginInstallParameters{}),

	// GET /plugins/{name}/json
	// Plugin

	// DELETE /plugins/{name}
	reflect.TypeOf(PluginRemoveParameters{}),

	// POST /plugins/{name}/enable
	reflect.TypeOf(PluginEnableParameters{}),

	// POST /plugins/{name}/disable
	reflect.TypeOf(PluginDisableParameters{}),

	// POST /plugins/{name}/upgrade
	// []PluginConfigArgs
	reflect.TypeOf(PluginUpgradeParameters{}),

	// POST /plugins/create
	reflect.TypeOf(PluginCreateParameters{}),

	// POST /plugins/{name}/push

	// POST /plugins/{name}/set
	reflect.TypeOf(PluginConfigureParameters{}),

	// GET /version
	reflect.TypeOf(types.Version{}),

	// GET /volumes
	reflect.TypeOf(VolumesListParameters{}),
	reflect.TypeOf(VolumesListResponse{}),

	// POST /volumes/create
	reflect.TypeOf(VolumesCreateParameters{}),

	// POST /volumes/prune
	reflect.TypeOf(VolumesPruneParameters{}),
	reflect.TypeOf(types.VolumesPruneReport{}),

	// GET /volumes/(id)
	reflect.TypeOf(VolumeResponse{}),

	// DELETE /volumes/(id)

	//
	// Swarm API
	//

	// POST /swarm/init
	reflect.TypeOf(swarm.InitRequest{}),

	// POST /swarm/join
	reflect.TypeOf(swarm.JoinRequest{}),

	// POST /swarm/leave
	reflect.TypeOf(SwarmLeaveParameters{}),

	// GET /swarm
	reflect.TypeOf(swarm.Swarm{}),

	// GET /swarm/unlockkey
	reflect.TypeOf(SwarmUnlockResponse{}),

	// POST /swarm/update
	reflect.TypeOf(SwarmUpdateParameters{}),

	// POST /swarm/unlock
	reflect.TypeOf(SwarmUnlockParameters{}),

	//
	// Secrets API (swarm)
	//

	// GET /secrets
	// GET /secrets/(id)
	reflect.TypeOf(swarm.Secret{}),

	// POST /secrets/create
	reflect.TypeOf(SecretCreateResponse{}),

	//
	// Configs API (swarm)
	//

	// GET /configs
	// GET /configs/(id)
	reflect.TypeOf(SwarmConfig{}),
	reflect.TypeOf(swarm.ConfigReference{}),

	// POST /configs/create
	reflect.TypeOf(SwarmCreateConfigParameters{}),
	reflect.TypeOf(SwarmCreateConfigResponse{}),

	// POST /configs/(id)/update
	reflect.TypeOf(SwarmUpdateConfigParameters{}),
	reflect.TypeOf(swarm.ConfigSpec{}),

	// GET /services
	// GET /services/(id)
	reflect.TypeOf(swarm.Service{}),
	reflect.TypeOf(ServiceListParameters{}),

	// POST /services/create
	reflect.TypeOf(ServiceCreateParameters{}),
	reflect.TypeOf(types.ServiceCreateResponse{}),

	// POST /services/(id)/update
	reflect.TypeOf(ServiceUpdateParameters{}),
	reflect.TypeOf(types.ServiceUpdateResponse{}),

	// DELETE /services/(id)

	// GET /services/(id)/logs
	reflect.TypeOf(ServiceLogsParameters{}),

	// GET /tasks
	reflect.TypeOf(TasksListParameters{}),
	reflect.TypeOf(swarm.Task{}),

	// GET /nodes
	// GET /nodes/(id)
	reflect.TypeOf(swarm.Node{}),
	reflect.TypeOf(swarm.TLSInfo{}),

	// DELETE /nodes/(id)

	// POST /nodes/(id)/update
	reflect.TypeOf(swarm.NodeSpec{}),
}

func csType(t reflect.Type, opt bool) CSType {
	def, ok := CSCustomTypeMap[t]
	if !ok {
		def, ok = CSInboxTypesMap[t.Kind()]
	}

	if ok {
		return def
	}

	switch t.Kind() {
	case reflect.Array:
		return CSType{"", fmt.Sprintf("%s[]", csType(t.Elem(), false).Name), false}
	case reflect.Slice:
		return CSType{"System.Collections.Generic", fmt.Sprintf("IList<%s>", csType(t.Elem(), false).Name), false}
	case reflect.Map:
		if t.Elem() == EmptyStruct {
			return CSType{"System.Collections.Generic", fmt.Sprintf("IDictionary<%s, EmptyStruct>", csType(t.Key(), false).Name), false}
		}

		return CSType{"System.Collections.Generic", fmt.Sprintf("IDictionary<%s, %s>", csType(t.Key(), false).Name, csType(t.Elem(), false).Name), false}
	case reflect.Ptr:
		return csType(t.Elem(), true)
	case reflect.Struct:
		if m, ok := reflectedTypes[typeToKey(t)]; ok {
			// We have aliased this type. Return it as a reference.
			return CSType{"", m.Name, false}
		}
		return CSType{"", t.Name(), false}
	case reflect.Interface:
		return CSType{"", "object", false}
	default:
		panic(fmt.Errorf("cannot convert type %s", t))
	}
}

func ultimateType(t reflect.Type) reflect.Type {
	for {
		switch t.Kind() {
		case reflect.Array, reflect.Chan, reflect.Map, reflect.Ptr, reflect.Slice:
			t = t.Elem()
		default:
			return t
		}
	}
}

func reflectTypeMembers(t reflect.Type, m *CSModelType) {
	for i := 0; i < t.NumField(); i++ {
		f := t.Field(i)

		switch f.Type.Kind() {
		case reflect.Func, reflect.Uintptr:
			continue
		}

		if f.Type.Kind() == reflect.Struct && f.Type.Name() == "" {
			// TODO: Inline struct definitions. Probably need to write an inline class named the property name?
			continue
		}

		// If the type is anonymous we need to inline its values to this model.
		if f.Anonymous {
			clen := len(m.Constructors)
			if clen == 0 {
				// We need to add a default constructor and a custom one since its the first time.
				m.Constructors = append(m.Constructors, CSConstructor{}, CSConstructor{})
			}

			ut := ultimateType(f.Type)
			reflectType(ut)
			newType := reflectedTypes[typeToKey(ut)]
			if newType == nil {
				panic(fmt.Sprintf("Failed to reflect ultimate type (%s) for anonymous member (%s) on type (%s)", ut, f.Name, t))
			}

			m.Constructors[1].Parameters = append(m.Constructors[1].Parameters, CSParameter{newType, f.Name})

			// Now we need to add in all of the inherited types parameters
			for _, p := range newType.Properties {
				m.Properties = append(m.Properties, p)
			}
		} else {
			// If we are referencing a struct that isnt inline or anonymous we need to update it too.
			if ut := ultimateType(f.Type); ut.Kind() == reflect.Struct {
				if _, ok := CSInboxTypesMap[f.Type.Kind()]; !ok {
					if _, ok := CSCustomTypeMap[f.Type]; !ok {
						reflectType(ut)
					}
				}
			}

			var typeCustomizations *CSModelType
			var hasTypeCustomizations bool
			typeCustomizations, hasTypeCustomizations = typesToDisambiguate[typeToKey(t)]

			// If the json tag says to omit we skip generation.
			jsonTag := strings.Split(f.Tag.Get("json"), ",")
			if jsonTag[0] == "-" {
				continue
			}

			// Create our new property.
			csProp := CSProperty{Name: f.Name, Type: csType(f.Type, false)}

			jsonName := f.Name
			if jsonTag[0] != "" {
				jsonName = jsonTag[0]
			}

			if hasTypeCustomizations {
				for _, p := range typeCustomizations.Properties {
					if f.Name == p.Name {
						// We have a custom property modification. Change the property.
						csProp.Type = p.Type
						break
					}
				}
			}

			if restTag, err := RestTagFromString(f.Tag.Get("rest")); err == nil && restTag.In != body {
				if restTag.Name == "" {
					restTag.Name = strings.ToLower(f.Name)
				}

				a := CSAttribute{Type: CSType{"", "QueryStringParameter", false}}
				a.Arguments = append(
					a.Arguments,
					CSArgument{
						restTag.Name,
						CSInboxTypesMap[reflect.String]},
					CSArgument{strconv.FormatBool(restTag.Required),
						CSInboxTypesMap[reflect.Bool]})

				switch f.Type.Kind() {
				case reflect.Bool:
					a.Arguments = append(a.Arguments, CSArgument{Value: "typeof(BoolQueryStringConverter)"})
				case reflect.Slice, reflect.Array:
					a.Arguments = append(a.Arguments, CSArgument{Value: "typeof(EnumerableQueryStringConverter)"})
				case reflect.Map:
					a.Arguments = append(a.Arguments, CSArgument{Value: "typeof(MapQueryStringConverter)"})
				}

				csProp.IsOpt = !restTag.Required
				csProp.Attributes = append(csProp.Attributes, a)
				csProp.DefaultValue = restTag.Default
			} else {
				a := CSAttribute{Type: CSType{"", "DataMember", false}}
				a.NamedArguments = append(a.NamedArguments, CSNamedArgument{"Name", CSArgument{jsonName, CSInboxTypesMap[reflect.String]}})
				a.NamedArguments = append(a.NamedArguments, CSNamedArgument{"EmitDefaultValue", CSArgument{strconv.FormatBool(false), CSInboxTypesMap[reflect.Bool]}})
				csProp.IsOpt = f.Type.Kind() == reflect.Ptr
				csProp.Attributes = append(csProp.Attributes, a)
			}

			if hasTypeCustomizations {
				for _, p := range typeCustomizations.Properties {
					if f.Name == p.Name {
						for _, ca := range p.Attributes {
							// We have a custom property attribute. Append it.
							csProp.Attributes = append(csProp.Attributes, ca)
						}
						break
					}
				}
			}

			// Lastly assign the property to our type.
			m.Properties = append(m.Properties, csProp)
		}
	}
}

func reflectType(t reflect.Type) {
	k := typeToKey(t)
	var activeType *CSModelType
	var alreadyInserted bool
	if activeType, alreadyInserted = reflectedTypes[k]; alreadyInserted {
		if activeType.IsStarted {
			return
		}
	} else if _, ok := CSCustomTypeMap[t]; ok {
		return
	}

	if t.Name() == "" {
		panic("Unable to reflect a type with no name")
	}

	name := t.Name()
	if n, ok := typesToDisambiguate[k]; ok {
		if n.Name != "" {
			name = n.Name
		}
	}

	if activeType == nil {
		activeType = NewModel(name, t.String())
	}

	activeType.IsStarted = true

	if !alreadyInserted {
		reflectedTypes[k] = activeType
	}

	reflectTypeMembers(t, activeType)
}

func main() {
	argsLen := len(os.Args)
	sourcePath := ""
	if argsLen >= 2 {
		sourcePath = os.Args[1]
		fmt.Println(sourcePath)
		if _, err := os.Stat(sourcePath); err != nil {
			if os.IsNotExist(err) {
				panic(sourcePath + ", is not a valid directory.")
			}
		}
	} else {
		sourcePath, _ = os.Getwd()
	}

	// Delete any previously generated files.
	if files, err := ioutil.ReadDir(sourcePath); err != nil {
		panic(err)
	} else {
		for _, file := range files {
			if strings.HasSuffix(file.Name(), ".Generated.cs") {
				if err := os.Remove(path.Join(sourcePath, file.Name())); err != nil {
					panic(err)
				}
			}
		}
	}

	// Reflect the specific docker types we are about and their dependencies.
	for _, t := range dockerTypesToReflect {
		reflectType(t)
	}

	for k, v := range reflectedTypes {
		if _, e := os.Stat(path.Join(sourcePath, v.Name+".Generated.cs")); e == nil {
			panic(fmt.Sprintf("File: (%s.Generated.cs) already exists. Failed to write key same name for key: (%s) type: (%s).", v.Name, k, v.SourceName))
		}

		f, err := ioutil.TempFile(sourcePath, "ser")
		if err != nil {
			panic(err)
		}

		defer f.Close()

		b := bufio.NewWriter(f)
		v.Write(b)
		err = b.Flush()
		if err != nil {
			os.Remove(f.Name())
			panic(err)
		}

		f.Close()
		os.Rename(f.Name(), path.Join(sourcePath, v.Name+".Generated.cs"))
	}
}

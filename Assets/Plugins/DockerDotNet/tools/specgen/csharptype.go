package main

import (
	"fmt"
	"io"
	"reflect"
	"sort"
	"strings"
	"time"

	"github.com/docker/docker/api/types/registry"
)

// EmptyStruct is a type that represents a struct with no exported values.
var EmptyStruct = reflect.TypeOf(struct{}{})

// CSInboxTypesMap is a map from Go type kind to C# type.
var CSInboxTypesMap = map[reflect.Kind]CSType{
	reflect.Int:   {"", "long", true}, // In practice most clients are 64bit so in go Int will be too.
	reflect.Int8:  {"", "sbyte", true},
	reflect.Int16: {"", "short", true},
	reflect.Int32: {"", "int", true},
	reflect.Int64: {"", "long", true},

	reflect.Uint:   {"", "ulong", true}, // In practice most clients are 64bit so in go Uint will be too.
	reflect.Uint8:  {"", "byte", true},
	reflect.Uint16: {"", "ushort", true},
	reflect.Uint32: {"", "uint", true},
	reflect.Uint64: {"", "ulong", true},

	reflect.String: {"", "string", false},

	reflect.Bool: {"", "bool", true},

	reflect.Float32: {"", "float", true},
	reflect.Float64: {"", "double", true},
}

// CSCustomTypeMap is a map from Go reflected types to C# types.
var CSCustomTypeMap = map[reflect.Type]CSType{
	reflect.TypeOf(time.Time{}):         {"System", "DateTime", true},
	reflect.TypeOf(registry.NetIPNet{}): {"", "string", false},
	EmptyStruct:                         {"", "BUG_IN_CONVERSION", false},
}

// CSArgument is a type that represents a C# argument that can
// be passed to a function/constructor.
type CSArgument struct {
	Value string
	Type  CSType
}

func (a CSArgument) String() string {
	if a.Type.Name == "string" {
		return fmt.Sprintf("\"%s\"", a.Value)
	}

	return a.Value
}

// CSNamedArgument is a type that represents a C# named argument that
// can take the form of Name = Argument to a function/constructor.
type CSNamedArgument struct {
	Name     string
	Argument CSArgument
}

func (a CSNamedArgument) String() string {
	return fmt.Sprintf("%s = %s", a.Name, a.Argument)
}

// CSAttribute is a type that represents a C# attribute.
type CSAttribute struct {
	Type           CSType
	Arguments      []CSArgument
	NamedArguments []CSNamedArgument
}

func (a CSAttribute) String() string {
	s := fmt.Sprintf("[%s", a.Type.Name)

	lenA := len(a.Arguments)
	lenN := len(a.NamedArguments)
	hasArgs := lenA > 0 || lenN > 0
	if hasArgs {
		s += "("
	}

	for i, a := range a.Arguments {
		s += a.String()

		if i != lenA-1 {
			s += ", "
		}
	}

	for i, n := range a.NamedArguments {
		s += n.String()

		if i != lenN-1 {
			s += ", "
		}
	}

	if hasArgs {
		s += ")"
	}

	return s + "]"
}

// CSType is a type that represents a C# type.
type CSType struct {
	Namespace  string
	Name       string
	IsNullable bool
}

// CSParameter is a type that represents a parameter declaration of a C# parameter to a function/constructor.
type CSParameter struct {
	Type *CSModelType
	Name string
}

func (p CSParameter) toString() string {
	return fmt.Sprintf("%s %s", p.Type.Name, p.Name)
}

// CSConstructor is a type that represents a constructor declaration in C#.
type CSConstructor struct {
	Parameters []CSParameter
}

// CSProperty is a type that represents a property declaration in C#.
type CSProperty struct {
	Name         string
	Type         CSType
	IsOpt        bool
	Attributes   []CSAttribute
	DefaultValue string
}

// CSModelType is a type that represents a reflected type to generate a C# model for.
type CSModelType struct {
	Name         string
	SourceName   string
	Constructors []CSConstructor
	Properties   []CSProperty
	Attributes   []CSAttribute
	// IsStarted is used to signify if the model type has started reflection
	// yet. it is possible that given the recursive nature that it not be
	// completed but as long as this is true we will not attempt to generate the
	// type more than once.
	IsStarted bool
}

// NewModel creates a new model type with valid slices
func NewModel(name, sourceName string) *CSModelType {
	s := CSModelType{
		Name:       name,
		SourceName: sourceName,
	}

	s.Attributes = append(s.Attributes, CSAttribute{Type: CSType{"System.Runtime.Serialization", "DataContract", false}})

	return &s
}

// Write the specific model type to the io writer given.
func (t *CSModelType) Write(w io.Writer) {
	usings := calcUsings(t)
	for _, u := range usings {
		fmt.Fprintf(w, "using %s;\n", u)
	}

	fmt.Fprintln(w, "")

	fmt.Fprintln(w, "namespace Docker.DotNet.Models")
	fmt.Fprintln(w, "{")

	writeClass(w, t)

	fmt.Fprintln(w, "}")
}

func calcUsings(t *CSModelType) []string {
	added := make(map[string]bool)
	var usings []string

	for _, a := range t.Attributes {
		usings = safeAddUsing(a.Type.Namespace, usings, added)
	}

	for _, o := range t.Properties {
		usings = safeAddUsing(o.Type.Namespace, usings, added)
		for _, p := range o.Attributes {
			usings = safeAddUsing(p.Type.Namespace, usings, added)
		}
	}

	// C# convertion is that 'System' usings are first. Sort them as if they are
	// the 'least' significant order so they appear first in the output.
	sort.Slice(usings, func(i, j int) bool {
		ip := strings.HasPrefix(usings[i], "System")
		jp := strings.HasPrefix(usings[j], "System")
		if ip && jp {
			// System sort them.
		} else if ip {
			// ip has 'System' prefix and jp does not.
			return true
		} else if jp {
			// jp has 'System' prefix and ip does not.
			return false
		}

		return strings.Compare(usings[i], usings[j]) < 0
	})

	return usings
}

func safeAddUsing(using string, usings []string, added map[string]bool) []string {
	if using != "" {
		if _, ok := added[using]; !ok {
			added[using] = true
			return append(usings, using)
		}
	}

	return usings
}

func writeClass(w io.Writer, t *CSModelType) {
	for _, a := range t.Attributes {
		fmt.Fprintf(w, "    %s\n", a)
	}

	fmt.Fprintf(w, "    public class %s // (%s)\n", t.Name, t.SourceName)
	fmt.Fprintln(w, "    {")

	if len(t.Constructors) > 0 {
		writeConstructors(w, t.Name, t.Constructors)

		if len(t.Properties) > 0 {
			fmt.Fprintln(w, "")
		}
	}

	if len(t.Properties) > 0 {
		writeProperties(w, t.Properties)
	}

	fmt.Fprintln(w, "    }")
}

func writeConstructors(w io.Writer, typeName string, constructors []CSConstructor) {
	l := len(constructors)
	for i, c := range constructors {
		fmt.Fprintf(w, "        public %s(", typeName)

		plen := len(c.Parameters)
		for pi, p := range c.Parameters {
			fmt.Fprintf(w, p.toString())

			if pi != plen-1 {
				fmt.Fprint(w, ", ")
			}
		}

		fmt.Fprintf(w, ")\n")
		fmt.Fprintln(w, "        {")

		// If we had parameters we need to handle the copy of the data for the structs.
		if plen > 0 {
			for pi, p := range c.Parameters {
				fmt.Fprintf(w, "            if (%s != null)\n", p.Name)
				fmt.Fprintln(w, "            {")

				// Assign each of the types.
				for _, elem := range p.Type.Properties {
					fmt.Fprintf(w, "                this.%s = %s.%s;\n", elem.Name, p.Name, elem.Name)
				}

				fmt.Fprintln(w, "            }")

				if pi != plen-1 {
					fmt.Fprintln(w, "")
				}
			}
		}

		fmt.Fprintln(w, "        }")
		if i != l-1 {
			fmt.Fprintln(w, "")
		}
	}
}

func writeProperties(w io.Writer, properties []CSProperty) {
	len := len(properties)
	for i, p := range properties {
		for _, a := range p.Attributes {
			fmt.Fprintf(w, "        %s\n", a)
		}

		if p.Type.IsNullable && p.IsOpt {
			fmt.Fprintf(w, "        public %s? %s { get; set; }", p.Type.Name, p.Name)
		} else {
			fmt.Fprintf(w, "        public %s %s { get; set; }", p.Type.Name, p.Name)
		}

		if p.DefaultValue != "" {
			fmt.Fprintf(w, " = %s;", p.DefaultValue)
		}

		fmt.Fprintln(w)

		if i != len-1 {
			fmt.Fprintln(w, "")
		}
	}
}

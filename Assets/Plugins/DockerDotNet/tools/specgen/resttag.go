package main

import (
	"errors"
	"strings"
)

const (
	header = "header"
	body   = "body"
	query  = "query"

	requiredTag = "required"
)

// RestTag is a type that represents the valid values of a 'rest' struct tag.
type RestTag struct {
	In       string
	Name     string
	Required bool
	Default  string
}

// RestTagFromString is a method to parse a 'rest' struct tag to a resulting RestTag struct.
// This can take the form of rest:in,name,required
func RestTagFromString(tag string) (RestTag, error) {
	if tag == "" {
		return RestTag{}, errors.New("Nil or empty tag string")
	}

	entries := strings.Split(tag, ",")
	elen := len(entries)

	r := RestTag{In: "", Name: "", Required: false}
	if elen >= 1 {
		r.In = entries[0]
		switch r.In {
		case header:
		case body:
		case query:
		default:
			return RestTag{}, errors.New("Incorrect 'in' value: " + r.In)
		}
	}

	if elen >= 2 {
		r.Name = entries[1]
	}

	if elen >= 3 && entries[2] == "required" {
		r.Required = true
	}

	if elen >= 4 {
		r.Default = entries[3]
	}

	return r, nil
}

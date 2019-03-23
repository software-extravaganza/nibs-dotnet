require 'ffi'
require 'json'

module App
  class NativeBridge extend FFI::Library

    ffi_lib_flags :now, :global
    # ffi_lib 'C:\Users\philc\Documents\src\bunkerware\ffi\Library\bin\release\netcoreapp3.0\win-x64\native\Library.dll'
    module Parent extend FFI::Library
     
    end
    
    class NativeAgent extend FFI::Library
      def self.hook_up(calling_class, desired_parent_class)
        calling_class.send :include , desired_parent_class
      end

      ffi_lib_flags :now, :global
      def self.attach_internal_method(lib_path, parameters, native_method)
        begin
          ffi_lib lib_path
          return_type = native_method["ReturnType"]
          attach_function(native_method["Name"].to_sym, parameters, return_type["Name"].to_sym)
          #puts "Registered  #{self.class}.#{native_method["Name"].to_sym}(#{parameters}) -> #{native_method["ReturnType"]["Name"].to_sym}"
        rescue Object => e
          puts "Could not attach  #{self.class}.#{native_method["Name"].to_sym}(#{parameters}) -> #{native_method["ReturnType"]["Name"].to_sym}, #{e.message}"
        end
      end
    end

    def self.prepareAssemblies(parent, native_assemblies, assembly_path)
      native_assemblies.each do |child_assembly|
        begin
          prepareNamespaces(parent, child_assembly["Namespaces"], assembly_path)
        end
      end
    end

    def self.prepareNamespaces(parent, native_namespaces, assembly_path)
      native_namespaces.each do |child_namepace|
        begin
          prepareNamespace(parent, child_namepace, assembly_path)
        end
      end
    end

    def self.prepareNamespace(parent, native_namespace, assembly_path)
      current_namespace = parent.const_set(native_namespace["Name"], Module.new)
      prepareNamespaces(parent, native_namespace["Namespaces"], assembly_path)
      native_namespace["Classes"].each do |native_class|
        begin
          prepareClass(current_namespace, native_class, assembly_path)
        end
      end
    end

    def self.prepareClass(parent, native_class, assembly_path)
      internal_class_container = Class.new(NativeAgent)
      external_class_container = Class.new
      # NativeAgent.hook_up(internal_class_container, Parent)
      internal_class = parent.const_set("#{native_class["Name"]}_internal", internal_class_container)
      external_class = parent.const_set(native_class["Name"], external_class_container)
      native_class["Methods"].each do |native_method|
        begin
          prepareMethod(internal_class, external_class, native_method, assembly_path)
        end
      end
    end

    def self.getMethodParameters(native_method)
      discovered_parameters = []
      native_method["Parameters"].each do |native_parameter|
        begin
          type = native_parameter["Type"]
          discovered_parameters.push(type["Name"].to_sym)
        end
      end
      return discovered_parameters
    end

    def self.getTypeName(object_hash, key)
      if object_hash == nil || !object_hash.has_key?(key)
        return ""
      end

      type_value = object_hash[key]
      if type_value == nil || !type_value.has_key?("Name")
        return ""
      end

      return type_value["Name"]
    end

    def self.prepareMethod(internal_class_container, external_class_container, native_method, assembly_path)
      parameters = getMethodParameters(native_method)
      lib_path = assembly_path #File.join(File.absolute_path(File.dirname(__FILE__)), "../Library/bin/release/netcoreapp3.0/win-x64/native/Library.dll")
      internal_class_container.attach_internal_method(lib_path, parameters, native_method)
      external_class_container.define_singleton_method(native_method["Name"]) do |*args, &block|
        method = internal_class_container.method(native_method["Name"])
        pre_result = method.call(*args)
        return_type = NativeBridge.getTypeName(native_method, "ReturnType")
        intended_return_type = NativeBridge.getTypeName(native_method, "IntendedReturnType")
        if return_type == "pointer" && intended_return_type == "string" 
          result = String.new(pre_result.read_string)
        else
          result = pre_result
        end

        if native_method["ReturnType"] == "pointer"
          NativeBridge.free_ptr(resultprt)
        end

        return result
      end
    end

    def self.process(lib_path)
      ffi_lib lib_path
      functions = [
        [:free_ptr, [:pointer], :void],
        [:get_native_metadata, [:int], :pointer],
      ]
    
      functions.each do |func|
        begin
          attach_function(*func)
        rescue Object => e
          puts "Could not attach #{func}, #{e.message}"
        end
      end

      resultprt = NativeBridge.get_native_metadata(2)
      result = String.new(resultprt.read_string)
      NativeBridge.free_ptr(resultprt)
      puts "###############################"
      puts "#####   NATIVE METADATA   #####"
      puts "###############################"
      puts result
      puts "###############################"
      
      json_metadata = JSON.parse(result)

      if json_metadata.has_key?("Error")
        puts "Error using native bridge: #{json_metadata["Error"]}"
      else
        NativeBridge::prepareAssemblies(NativeBridge, json_metadata["Data"], lib_path)
      end
    end
    
  end
end








entry_lib = File.join(File.absolute_path(File.dirname(__FILE__)), "../Library/bin/release/netcoreapp3.0/win-x64/native/Library.dll");
App::NativeBridge.process(entry_lib)

puts App::NativeBridge::Library::NativeCore.subtract(2, 5)
puts App::NativeBridge::Library::NativeCore.append("me", "too")


















    #attach_function(func["Name"].to_sym, discovered_parameters, func["ReturnType"].to_sym)
    # rescue Object => e
    #   puts "Could not attach #{func["Name"].to_sym} (#{discovered_parameters}) -> #{func["ReturnType"].to_sym}, #{e.message}"
    # end







# m1 = Object.const_set("Example1", Module.new)
# c1 = Class.new
# NativeAgent.hook_up(c1, Parent)
# m2 = m1.const_set("Example2", c1)
# m2.define_singleton_method("TestMethod") { "example1_with #{self.new.go}" }

# #puts Example1::Example2.TestMethod
# puts App::Lib.public_instance_methods
# puts App::Lib.constants

#puts Lib::Library.NativeCore_internal.subtract(2, 5)

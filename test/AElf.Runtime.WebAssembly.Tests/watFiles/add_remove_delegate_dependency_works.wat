(module
	(import "seal0" "add_delegate_dependency" (func $add_delegate_dependency (param i32)))
	(import "seal0" "remove_delegate_dependency" (func $remove_delegate_dependency (param i32)))
	(import "env" "memory" (memory 1 1))
	(func (export "call")
		(call $add_delegate_dependency (i32.const 0))
		(call $add_delegate_dependency (i32.const 32))
		(call $remove_delegate_dependency (i32.const 32))
	)
	(func (export "deploy"))

	;;  hash1 (32 bytes)
	(data (i32.const 0)
		"\01\01\01\01\01\01\01\01\01\01\01\01\01\01\01\01"
		"\01\01\01\01\01\01\01\01\01\01\01\01\01\01\01\01"
	)

	;;  hash2 (32 bytes)
	(data (i32.const 32)
		"\02\02\02\02\02\02\02\02\02\02\02\02\02\02\02\02"
		"\02\02\02\02\02\02\02\02\02\02\02\02\02\02\02\02"
	)
)
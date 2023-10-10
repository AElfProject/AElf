(module
	(import "seal0" "seal_set_code_hash" (func $seal_set_code_hash (param i32) (result i32)))
	(import "env" "memory" (memory 1 1))
	(func $assert (param i32)
		(block $ok
			(br_if $ok
				(get_local 0)
			)
			(unreachable)
		)
	)
	(func (export "call")
		(local $exit_code i32)
		(set_local $exit_code
			(call $seal_set_code_hash (i32.const 0))
		)
		(call $assert
			(i32.eq (get_local $exit_code) (i32.const 0)) ;; ReturnCode::Success
		)
	)

	(func (export "deploy"))

	;; Hash of code.
	(data (i32.const 0)
		"\11\11\11\11\11\11\11\11\11\11\11\11\11\11\11\11"
		"\11\11\11\11\11\11\11\11\11\11\11\11\11\11\11\11"
	)
)
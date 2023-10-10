(module
	(import "seal0" "instantiation_nonce" (func $nonce (result i64)))
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
		(call $assert
			(i64.eq (call $nonce) (i64.const 995))
		)
	)
	(func (export "deploy"))
)
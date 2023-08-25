(module
	(import "seal0" "sr25519_verify" (func $sr25519_verify (param i32 i32 i32 i32) (result i32)))
	(import "env" "memory" (memory 1 1))
	(func (export "call")
		(drop
			(call $sr25519_verify
				(i32.const 0) ;; Pointer to signature.
				(i32.const 64) ;; Pointer to public key.
				(i32.const 16) ;; message length.
				(i32.const 96) ;; Pointer to message.
			)
		)
	)
	(func (export "deploy"))

	;; Signature (64 bytes)
	(data (i32.const 0)
		"\01\01\01\01\01\01\01\01\01\01\01\01\01\01\01\01"
		"\01\01\01\01\01\01\01\01\01\01\01\01\01\01\01\01"
		"\01\01\01\01\01\01\01\01\01\01\01\01\01\01\01\01"
		"\01\01\01\01\01\01\01\01\01\01\01\01\01\01\01\01"
	)

	;;  public key (32 bytes)
	(data (i32.const 64)
		"\01\01\01\01\01\01\01\01\01\01\01\01\01\01\01\01"
		"\01\01\01\01\01\01\01\01\01\01\01\01\01\01\01\01"
	)

	;;  message. (16 bytes)
	(data (i32.const 96)
		"\01\01\01\01\01\01\01\01\01\01\01\01\01\01\01\01"
	)
)
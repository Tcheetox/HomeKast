import React from 'react'

import { Dropdown } from 'react-bootstrap'

export default function DropPicker({ name, value, values, setValue }) {
  return (
    <div className='drop-picker'>
      <div className='label'>{name}</div>
      <Dropdown onSelect={setValue}>
        <Dropdown.Toggle id='dropdown-toggle' size='sm' variant='secondary'>
          {value ?? 'Undefined'}
        </Dropdown.Toggle>
        <Dropdown.Menu>
          {values.map(v => (
            <Dropdown.Item key={v} eventKey={v}>
              {v}
            </Dropdown.Item>
          ))}
        </Dropdown.Menu>
      </Dropdown>
    </div>
  )
}

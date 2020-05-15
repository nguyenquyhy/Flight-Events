import * as React from 'react';
import styled from 'styled-components';
import { ButtonGroup, Button, FormGroup, Input, Label } from 'reactstrap';
import { MapTileType } from '../maps/IMap';

interface Props {
    isDark: boolean;
    onIsDarkChanged: (isDark: boolean) => void;

    dimension: "2D" | "3D";
    onDimensionChanged: (dimension: "2D" | "3D") => void;

    tileType: MapTileType;
    onTileTypeChanged: (tileType: MapTileType) => void;
}

interface State {
    expand: boolean;
}

export default class Display extends React.Component<Props, State> {
    constructor(props: Props) {
        super(props);

        this.state = {
            expand: false
        }

        this.handleIsDark = this.handleIsDark.bind(this);

        this.handleMap2D = this.handleMap2D.bind(this);
        this.handleMap3D = this.handleMap3D.bind(this);

        this.handleOpenStreetMap = this.handleOpenStreetMap.bind(this);
        this.handleOpenTopoMap = this.handleOpenTopoMap.bind(this);
        this.handleEsriWorldImagery = this.handleEsriWorldImagery.bind(this);
        this.handleEsriTopo = this.handleEsriTopo.bind(this);
        this.handleUsVfrSectional = this.handleUsVfrSectional.bind(this);

        this.handleToggle = this.handleToggle.bind(this);
    }

    private handleIsDark() {
        this.props.onIsDarkChanged(!this.props.isDark);
    }

    private handleMap2D() {
        this.props.onDimensionChanged("2D");
    }

    private handleMap3D() {
        this.props.onDimensionChanged("3D");
    }

    private handleOpenStreetMap() {
        this.props.onTileTypeChanged(MapTileType.OpenStreetMap);
    }

    private handleOpenTopoMap() {
        this.props.onTileTypeChanged(MapTileType.OpenTopoMap);
    }

    private handleEsriWorldImagery() {
        this.props.onTileTypeChanged(MapTileType.EsriWorldImagery);
    }

    private handleEsriTopo() {
        this.props.onTileTypeChanged(MapTileType.EsriTopo);
    }

    private handleUsVfrSectional() {
        this.props.onTileTypeChanged(MapTileType.UsVfrSectional);
    }

    private handleToggle() {
        this.setState({ expand: !this.state.expand });
    }

    public render() {
        return <>
            {!this.state.expand ?
                <CollapsedDiv onClick={this.handleToggle} /> :
                <ExpandedDiv>
                    <CollapsedButton className="btn" onClick={this.handleToggle}><i className={"fas " + (this.state.expand ? "fa-chevron-up" : "fa-chevron-down")}></i></CollapsedButton>
                    <StyledFormGroup check>
                        <Input type="checkbox" name="checkIsDark" id="checkIsDark" checked={this.props.isDark} onChange={this.handleIsDark} />
                        <Label for="checkIsDark" check>Dark Mode</Label>
                    </StyledFormGroup>
                    <TypeWrapper className="btn-group-vertical">
                        <ButtonGroup>
                            <Button className="btn btn-light" active={this.props.dimension === "2D"} onClick={this.handleMap2D}>2D</Button>
                            <Button className="btn btn-light" active={this.props.dimension === "3D"} onClick={this.handleMap3D}>3D</Button>
                        </ButtonGroup>
                    </TypeWrapper>
                    <LayerWrapper className="btn-group-vertical">
                        <Button className="btn btn-light" active={this.props.tileType === MapTileType.OpenStreetMap} onClick={this.handleOpenStreetMap}>OpenStreetMap</Button>
                        <Button className="btn btn-light" active={this.props.tileType === MapTileType.OpenTopoMap} onClick={this.handleOpenTopoMap}>OpenTopoMap</Button>
                        <Button className="btn btn-light" active={this.props.tileType === MapTileType.EsriWorldImagery} onClick={this.handleEsriWorldImagery}>Esri Imagery</Button>
                        <Button className="btn btn-light" active={this.props.tileType === MapTileType.EsriTopo} onClick={this.handleEsriTopo}>Esri Topo</Button>
                        <Button className="btn btn-light" active={this.props.tileType === MapTileType.UsVfrSectional} onClick={this.handleUsVfrSectional}>US VFR</Button>
                    </LayerWrapper>
                </ExpandedDiv>
            }
        </>
    }
}

const CollapsedDiv = styled.div`
position: absolute;
top: 80px;
left: 10px;
z-index: 1000;
width: 40px;
height: 40px;
box-shadow: 0 1px 5px rgba(0,0,0,0.65);
border-radius: 4px;
background-color: rgba(255,255,255,0.8);
cursor: pointer;
`

const ExpandedDiv = styled.div`
position: absolute;
top: 80px;
left: 10px;
z-index: 1000;
box-shadow: 0 1px 5px rgba(0,0,0,0.65);
border-radius: 4px;
background-color: rgba(255,255,255,0.6);
cursor: pointer;
padding: 12px 10px 10px 10px;
`

const CollapsedButton = styled.button`
padding: 0;
font-size: 8px;
display: block;
position: absolute;
left: 0;
right: 0;
top: 0;
width: 100%;
height: 12px;
`

const StyledFormGroup = styled(FormGroup)`
display: block;
background-color: white;
width: 140px;
margin-top: 10px;
box-shadow: 0 1px 5px rgba(0,0,0,0.65);
border-radius: 4px;
padding: 4px 4px 4px 28px;
`

const TypeWrapper = styled.div`
display: block;
margin-top: 10px;
width: 140px;
box-shadow: 0 1px 5px rgba(0,0,0,0.65);
border-radius: 4px;
`;

const LayerWrapper = styled.div`
display: block;
margin-top: 10px;
width: 140px;
box-shadow: 0 1px 5px rgba(0,0,0,0.65);
border-radius: 4px;

button {
    display: block;
}
`;